using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Connectstate;
using Google.Protobuf;
using SpotifyNET.Enums;
using SpotifyNET.Interfaces;
using SpotifyNET.Models;

namespace SpotifyNET;

public class SpotifyRemoteState : ISpotifyRemoteState
{
    private readonly SpotifyRemoteConnect _spotifyRemoteConnect;

    internal SpotifyRemoteState(SpotifyRemoteConnect spotifyRemoteConnect,
        ISpotifyPlayer player)
    {
        Player = player ?? new InternalBarebonesNoPlayer(spotifyRemoteConnect.SpotifyClient.Config);
        _spotifyRemoteConnect = spotifyRemoteConnect;
        _spotifyRemoteConnect.ConnectionIdUpdated += SpotifyRemoteConnectOnConnectionIdUpdated;
    }

    private void SpotifyRemoteConnectOnConnectionIdUpdated(object sender, string e)
    {
        Player.State = InitState(Player.State);
    }

    private string _lastCommandSentByDeviceId;
    public ISpotifyPlayer? Player { get; }

    public async Task<RequestResult> OnRequest(SpotifyWebsocketRequest request)
    {
        if (Player == null)
            return RequestResult.DeviceNotFound;
        
        _lastCommandSentByDeviceId = request.Sender;
        if (!request.Command.TryGetProperty("endpoint", out var endpoinStr))
            return RequestResult.UnknownSendCommandResult;
        var endpointNullable = endpoinStr.GetString().StringToEndPoint();
        if (!endpointNullable.HasValue)
            return RequestResult.UnknownSendCommandResult;
        return await Task.Run(() =>
            Player.IncomingCommand(endpointNullable.Value, new CommandBody(request.Command)));
    }

    public async Task<byte[]> UpdateState(
        PutStateReason reason, 
        int playertime = -1)
    {
        var timestamp = (ulong) TimeHelper.CurrentTimeMillisSystem;
        if (playertime == -1)
            Player.PutState.HasBeenPlayingForMs = 0L;
        else
            Player.PutState.HasBeenPlayingForMs = (ulong) Math.Min((ulong) playertime,
                timestamp - Player.PutState.StartedPlayingAt);

        Player.PutState.PutStateReason = reason;
        Player.PutState.ClientSideTimestamp = timestamp;
        Player.PutState.Device.PlayerState = Player.State;
        var asBytes = Player.PutState.ToByteArray();
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
        {
            gzip.Write(asBytes, 0, asBytes.Length);
        }

        ms.Position = 0;
        var content = new StreamContent(ms);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");
        content.Headers.ContentEncoding.Add("gzip");

        var res = await _spotifyRemoteConnect.PutHttpClient
            .PutAsync($"/connect-state/v1/devices/{_spotifyRemoteConnect.SpotifyClient.Config.DeviceId}", content,
                CancellationToken.None);
        if (!res.IsSuccessStatusCode)
        {
            var bts = await res.Content.ReadAsByteArrayAsync();
            var str = System.Text.Encoding.UTF8.GetString(bts);
            throw new HttpRequestException(str);
        }

        //if (@break) Debugger.Break();
        return await res.Content.ReadAsByteArrayAsync();
    }

    private PlayerState InitState(PlayerState playerState = null)
    {
        if (playerState != null)
        {
            playerState.PlaybackSpeed = 1.0;
            playerState.SessionId = string.Empty;
            playerState.PlaybackId = string.Empty;
            playerState.Suppressions = new Suppressions();
            playerState.ContextRestrictions = new Restrictions();
            playerState.Options = new ContextPlayerOptions
            {
                RepeatingTrack = false,
                ShufflingContext = false,
                RepeatingContext = false
            };
            playerState.Position = 0;
            playerState.PositionAsOfTimestamp = 0;
            playerState.IsPlaying = false;
            playerState.IsSystemInitiated = true;
            return playerState;
        }

        Player.PutState.Device.DeviceInfo.Name = _spotifyRemoteConnect.SpotifyClient.Config.DeviceName;
        Player.PutState.Device.DeviceInfo.DeviceId = _spotifyRemoteConnect.SpotifyClient.Config.DeviceId;

        return new PlayerState
        {
            PlaybackSpeed = 1.0,
            SessionId = string.Empty,
            PlaybackId = string.Empty,
            Suppressions = new Suppressions(),
            ContextRestrictions = new Restrictions(),
            Options = new ContextPlayerOptions
            {
                RepeatingTrack = false,
                ShufflingContext = false,
                RepeatingContext = false
            },
            Position = 0,
            PositionAsOfTimestamp = 0,
            IsPlaying = false,
            IsSystemInitiated = true
        };
    }

    internal void Close()
    {
        _spotifyRemoteConnect.ConnectionIdUpdated -= SpotifyRemoteConnectOnConnectionIdUpdated;
    }
}