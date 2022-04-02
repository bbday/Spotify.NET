using System.Threading.Tasks;
using Connectstate;
using SpotifyNET.Enums;
using SpotifyNET.Interfaces;
using SpotifyNET.OneTimeStructures;

namespace SpotifyNET.Models;

internal class InternalBarebonesNoPlayer : ISpotifyPlayer
{
    internal InternalBarebonesNoPlayer(SpotifyConfig config)
    {
        PutState = new PutStateRequest
        {
            MemberType = MemberType.ConnectState,
            Device = new Device
            {
                DeviceInfo = new DeviceInfo()
                {
                    CanPlay = false,
                    Volume = 65536,
                    Name = config.DeviceName,
                    DeviceId = config.DeviceId,
                    DeviceType = DeviceType.Computer,
                    DeviceSoftwareVersion = "Spotify-11.1.0",
                    SpircVersion = "3.2.6",
                    Capabilities = new Capabilities
                    {
                        CanBePlayer = false,
                        GaiaEqConnectId = true,
                        SupportsLogout = true,
                        VolumeSteps = 64,
                        IsObservable = true,
                        CommandAcks = true,
                        SupportsRename = false,
                        SupportsPlaylistV2 = true,
                        IsControllable = false,
                        SupportsCommandRequest = false,
                        SupportsTransferCommand = false,
                        SupportsGzipPushes = true,
                        NeedsFullPlayerState = false
                    }
                }
            }
        };
    }
    public PlayerState State { get; set; }
    public PutStateRequest PutState { get; set; }
    public Task<RequestResult> IncomingCommand(Endpoint endpoint, CommandBody? data)
    {
        throw new System.NotImplementedException();
    }
}