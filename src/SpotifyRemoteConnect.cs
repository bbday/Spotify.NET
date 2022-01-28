using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
    public SpotifyRemoteConnect(SpotifyClient spotifyClient)
    {
        SpotifyClient = spotifyClient;
        _waitForConnectionId = new AsyncManualResetEvent(false);
        SpotifyRemoteState = new SpotifyRemoteState(this);
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
            _conId = value;
            if (!string.IsNullOrEmpty(value) && IsConnected)
            {
                _waitForConnectionId.Set();
            }
            else
            {
                _waitForConnectionId.Reset();
            } 
            if(value != _conId)
                ConnectionIdUpdated?.Invoke(this, value);
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

    public async Task<Cluster> ConnectAsync(CancellationToken ct = default)
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
        
        return _latestCluster;
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
            
            var initial = await 
                SpotifyRemoteState.UpdateState(PutStateReason.NewDevice, SpotifyRemoteState.State);
            CurrentCluster = Cluster.Parser.ParseFrom(initial);
            ConnectionId = connId;
        }
        else
        {
            
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