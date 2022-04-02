#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using Connectstate;
using Nito.AsyncEx;
using SpotifyNET.Enums;
using SpotifyNET.Helpers;
using SpotifyNET.Interfaces;
using SpotifyNET.Models;
using Websocket.Client;
using Websocket.Client.Models;
using Timer = System.Timers.Timer;

namespace SpotifyNET;

public class SpotifyRemoteConnect : ISpotifyRemoteConnect
{
    private IDisposable[] _disposables = new IDisposable[3];
    private Timer _keepAliveTimer = new Timer(TimeSpan.FromSeconds(20).TotalMilliseconds);
    private Cluster _latestCluster;
    private WebsocketClient _socketClient;
    private AsyncManualResetEvent _waitForConnectionId;
    private string? _conId;

    internal HttpClient PutHttpClient { get; private set; }

    /// <summary>
    /// Create a new instance of SpotifyRemoteConnect. 
    /// </summary>
    /// <param name="spotifyClient"></param>
    /// <param name="spotifyPlayer">An implementation of <see cref="ISpotifyPlayer"/>. To handle remote commands.</param>
    public SpotifyRemoteConnect(SpotifyClient spotifyClient,
        ISpotifyPlayer? spotifyPlayer = null)
    {
        SpotifyClient = spotifyClient;
        _waitForConnectionId = new AsyncManualResetEvent(false);
        SpotifyRemoteState = new SpotifyRemoteState(this, spotifyPlayer);
    }

    public ISpotifyRemoteState SpotifyRemoteState
    {
        get;
    }
    public string? ConnectionId
    {
        get => _conId;
        private set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _waitForConnectionId.Set();
            }
            else
            {
                _waitForConnectionId.Reset();
            }

            if (value != _conId)
            {
                ConnectionIdUpdated?.Invoke(this, value);
                _conId = value;
            }
        }
    }

    public event EventHandler<string> ConnectionIdUpdated;

    public bool IsConnected =>
        !string.IsNullOrEmpty(ConnectionId) && (_socketClient?.IsRunning ?? false);
    public SpotifyClient SpotifyClient { get; }

    public Cluster CurrentCluster
    {
        get => _latestCluster;
        set
        {
            if (value != _latestCluster)
            {
                _latestCluster = value;
                ClusterUpdated?.Invoke(this, value);
            }
        }
    }

    public event EventHandler<Cluster> ClusterUpdated;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        var factory = new Func<ClientWebSocket>(() =>
        {
            var client = new ClientWebSocket
            {
                Options =
                {
                    KeepAliveInterval = TimeSpan.FromDays(48),
                    // Proxy = ...
                    // ClientCertificates = ...
                }
            };
            //client.Options.SetRequestHeader("Origin", "xxx");
            return client;
        });
        _socketClient = new WebsocketClient(await GetUrl(SpotifyClient, ct), factory);

        _disposables[0] = _socketClient.MessageReceived
            .Where(msg => msg.Text != null)
            .Where(msg => msg.Text.StartsWith("{"))
            .Subscribe(OnWebsocketMessage);
        _disposables[1] = _socketClient.DisconnectionHappened
            .Subscribe(OnWebsocketDisconnection);
        _disposables[2] =
            _socketClient.ReconnectionHappened.Subscribe(OnWebsocketReconnection);
        _keepAliveTimer.Elapsed += KeepAliveElapsed;

        _socketClient.ErrorReconnectTimeout = TimeSpan.FromSeconds(2);
        await _socketClient.Start()
            .ConfigureAwait(false);
        _keepAliveTimer.Start();
        await _waitForConnectionId.WaitAsync(ct);
    }


    private void OnWebsocketReconnection(ReconnectionInfo obj)
    {
        Debug.WriteLine($"Socket reconnected! {obj.Type.ToString()}");
    }

    private void KeepAliveElapsed(object sender, ElapsedEventArgs e)
    {
        _socketClient.Send("{\"type\":\"ping\"}");
    }

    private async void OnWebsocketDisconnection(DisconnectionInfo obj)
    {
        Debug.WriteLine("WS CLOSED: reason: " + obj.CloseStatusDescription);
        _socketClient.Url = await GetUrl(SpotifyClient);
    }

    private async void OnWebsocketMessage(ResponseMessage obj)
    {
        using var message = JsonDocument.Parse(obj.Text);
        if (!message.RootElement.TryGetProperty("headers", out var headersStructure))
        {
            Debug.WriteLine($"No headers: \r\n {obj.Text}");
            return;
        }

        var headers = headersStructure.Deserialize<Dictionary<string, string>>();

        if (headers?.ContainsKey("Spotify-Connection-Id") ?? false)
        {
            var connId =
                HttpUtility.UrlDecode(headers["Spotify-Connection-Id"],
                    Encoding.UTF8);
            Debug.WriteLine($"new con id: {connId}");

            PutHttpClient?.Dispose();
            PutHttpClient = new HttpClient(new LoggingHandler(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip
            }, SpotifyClient))
            {
                BaseAddress = new Uri(await ApResolver.GetClosestSpClient())
            };
            PutHttpClient.DefaultRequestHeaders.Add("X-Spotify-Connection-Id", connId);
            ConnectionId = connId;
            var initial = await
                SpotifyRemoteState.UpdateState(PutStateReason.NewDevice);
            CurrentCluster = Cluster.Parser.ParseFrom(initial);
        }
        else
        {
            Debug.WriteLine($"Incoming ws message..");
            using var jsonDocument = JsonDocument.Parse(obj.Text);
            var wsMessage = AdaptToWsMessage(jsonDocument);
            switch (wsMessage)
            {
                case SpotifyWebsocketMessage msg:
                    if (msg.Uri.StartsWith("hm://connect-state/v1/cluster"))
                    {
                        var update = ClusterUpdate.Parser.ParseFrom(msg.Payload);
                        CurrentCluster = update.Cluster;
                        //LatestCluster = update.Cluster;
                        var now = TimeHelper.CurrentTimeMillisSystem;

                        //long ts = update.Cluster.Timestamp - 3000; // Workaround
                        //if (!_spotifyClient.Config.DeviceId.Equals(update.Cluster.ActiveDeviceId) &&
                        //    _connectStateHolder.PutState.IsActive
                        //    && now > (long)_connectStateHolder.PutState.StartedPlayingAt && ts > (long)_connectStateHolder.PutState.StartedPlayingAt)
                        //    _connectStateHolder.NotActive();
                    }
                    break;
                case SpotifyWebsocketRequest req:
                    var result =
                        await Task.Run(() => SpotifyRemoteState.OnRequest(req));
                    SendReply(req.Key, result);
                    //Debug.WriteLine("Handled request. key: {0}, result: {1}", req.Key, result);
                    break;
            }
        }
    }

    private void SendReply(string key, RequestResult result)
    {
        var success = result == RequestResult.Success;
        var reply =
            $"{{\"type\":\"reply\", \"key\": \"{key.ToLower()}\", \"payload\": {{\"success\": {success.ToString().ToLowerInvariant()}}}}}";
        _socketClient.Send(reply);
    }

    private static ISpotifyWsMsg? AdaptToWsMessage(JsonDocument obj)
    {
        if (!obj.RootElement.TryGetProperty("type", out var type))
            return null;
        switch (type.GetString())
        {
            case "ping":
                return new Ping();
                //return new Ping();
                break;
            case "pong":
                return new Pong();
                break;
            case "request":
                {
                    Debug.Assert(obj != null, nameof(obj) + " != null");
                    var mid = obj.RootElement.GetProperty("message_ident").GetString();
                    var key = obj.RootElement.GetProperty("key").GetString();
                    if (!obj.RootElement.TryGetProperty("headers", out var headersStructure))
                    {
                        Debug.WriteLine($"No headers: \r\n {obj}");
                    }

                    var headers = headersStructure.Deserialize<Dictionary<string, string>>();
                    var payload = obj.RootElement.GetProperty("payload");

                    using var @in = new MemoryStream();
                    using var outputStream =
                        new MemoryStream(Convert.FromBase64String(payload.GetProperty("compressed").GetString()));
                    if (headers["Transfer-Encoding"]?.Equals("gzip") ?? false)
                    {
                        using var decompressionStream = new GZipStream(outputStream, CompressionMode.Decompress);
                        decompressionStream.CopyTo(@in);
                        Debug.WriteLine($"Decompressed");
                        var jsonStr = Encoding.Default.GetString(@in.ToArray());
                        using var jsonDoc = JsonDocument.Parse(jsonStr);
                        payload = jsonDoc.RootElement.Clone();
                    }

                    var pid = payload.GetProperty("message_id").GetInt32();
                    var sender = payload.GetProperty("sent_by_device_id").GetString();

                    var command = payload.GetProperty("command").Clone();
                    Debug.WriteLine("Received request. mid: {0}, key: {1}, pid: {2}, sender: {3}", mid, key, pid,
                        sender);
                    return new SpotifyWebsocketRequest(mid, pid, sender, command, key);
                }
                break;
            case "message":
                {
                    if (!obj.RootElement.TryGetProperty("headers", out var headersStructure))
                    {
                        Debug.WriteLine($"No headers: \r\n {obj}");
                    }

                    var headers = headersStructure.Deserialize<Dictionary<string, string>>();
                    var uri = obj.RootElement.GetProperty("uri").GetString();
                    byte[] decodedPayload = null;
                    if (obj.RootElement.TryGetProperty("payloads", out var payloadsObject))
                    {
                        using var payloads = payloadsObject.EnumerateArray();
                        var arr = payloads.ToImmutableArray();
                        if (headers.ContainsKey("Content-Type")
                            && (headers["Content-Type"].Equals("application/json") ||
                                headers["Content-Type"].Equals("text/plain")))
                        {
                            if (arr.Length > 1) throw new InvalidOperationException();
                            decodedPayload = Encoding.Default.GetBytes(arr[0].ToString());
                        }
                        else if (headers.Any())
                        {
                            var payloadsStr = new string[arr.Length];
                            for (var i = 0; i < arr.Length; i++) payloadsStr[i] = arr[i].ToString();
                            var x = string.Join("", payloadsStr);
                            using var @in = new MemoryStream();
                            using var outputStream = new MemoryStream(Convert.FromBase64String(x));
                            if (headers.ContainsKey("Transfer-Encoding")
                                && (headers["Transfer-Encoding"]?.Equals("gzip") ?? false))
                            {
                                using var decompressionStream =
                                    new GZipStream(outputStream, CompressionMode.Decompress);
                                decompressionStream.CopyTo(@in);
                                Debug.WriteLine("Decompressed");
                            }

                            decodedPayload = @in.ToArray();
                        }
                        else
                        {
                            Debug.WriteLine($"Unknown message; Possibly playlist update.. {uri}");
                        }
                    }
                    else
                    {
                        decodedPayload = new byte[0];
                    }

                    return new SpotifyWebsocketMessage(uri, headers, decodedPayload);
                }
            default:
                Debugger.Break();
                throw new NotImplementedException();
        }
    }

    public void Dispose()
    {
        _socketClient?.Dispose();
        _disposables[0]?.Dispose();
        _disposables[1]?.Dispose();
        _disposables[2]?.Dispose();
        _keepAliveTimer.Elapsed -= KeepAliveElapsed;
        _keepAliveTimer?.Dispose();
        (SpotifyRemoteState as SpotifyRemoteState)?.Close();
    }

    private static async ValueTask<Uri> GetUrl(ISpotifyClient client,
        CancellationToken ct = default)
    {
        var token = await client.GetBearerAsync(ct);
        var socketUrl = new Uri(
            $"wss://{(await ApResolver.GetClosestDealerAsync(ct)).Replace("https://", string.Empty)}/" +
            $"?access_token={token.AccessToken}");
        return socketUrl;
    }
}