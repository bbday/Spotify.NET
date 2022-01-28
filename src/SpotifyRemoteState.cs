using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Connectstate;
using Google.Protobuf;
using SpotifyNET.Interfaces;
using SpotifyNET.Models;

namespace SpotifyNET;

public class SpotifyRemoteState : ISpotifyRemoteState
{
    private readonly SpotifyRemoteConnect _spotifyRemoteConnect;

    internal SpotifyRemoteState(SpotifyRemoteConnect spotifyRemoteConnect)
    {
        _spotifyRemoteConnect = spotifyRemoteConnect;
        _spotifyRemoteConnect.ConnectionIdUpdated += SpotifyRemoteConnectOnConnectionIdUpdated;
        PutState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = true,
                    Volume = 65536,
                    Name = _spotifyRemoteConnect.SpotifyClient.Config.DeviceName,
                    DeviceId = _spotifyRemoteConnect.SpotifyClient.Config.DeviceId,
                    DeviceType = DeviceType.Computer,
                    DeviceSoftwareVersion = "Spotify-11.1.0",
                    SpircVersion = "3.2.6",
                    Capabilities = new Capabilities
                    {
                        CanBePlayer = true,
                        GaiaEqConnectId = true,
                        SupportsLogout = true,
                        VolumeSteps = 64,
                        IsObservable = true,
                        CommandAcks = true,
                        SupportsRename = false,
                        SupportsPlaylistV2 = true,
                        IsControllable = true,
                        SupportsCommandRequest = true,
                        SupportsTransferCommand = true,
                        SupportsGzipPushes = true,
                        NeedsFullPlayerState = false,
                        SupportedTypes =
                        {
                            "audio/episode",
                            "audio/track"
                        }
                    }
                }
            }
        };
    }

    private void SpotifyRemoteConnectOnConnectionIdUpdated(object sender, string e)
    {
        State = InitState(State);
    }

    public PlayerState State { get; private set; }
    public PutStateRequest PutState { get; private set; }

    public async Task<byte[]> UpdateState(
        PutStateReason reason, PlayerState st,
        int playertime = -1)
    {
        var timestamp = (ulong) TimeHelper.CurrentTimeMillisSystem;
        if (playertime == -1)
            PutState.HasBeenPlayingForMs = 0L;
        else
            PutState.HasBeenPlayingForMs = (ulong) Math.Min((ulong) playertime,
                timestamp - PutState.StartedPlayingAt);

        PutState.PutStateReason = reason;
        PutState.ClientSideTimestamp = timestamp;
        PutState.Device.PlayerState = st;
        var asBytes = PutState.ToByteArray();
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

        PutState.Device.DeviceInfo.Name = _spotifyRemoteConnect.SpotifyClient.Config.DeviceName;
        PutState.Device.DeviceInfo.DeviceId = _spotifyRemoteConnect.SpotifyClient.Config.DeviceId;

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