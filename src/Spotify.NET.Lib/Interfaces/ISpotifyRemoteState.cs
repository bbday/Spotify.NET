#nullable enable
using System.Threading.Tasks;
using Connectstate;
using SpotifyNET.Enums;

namespace SpotifyNET.Interfaces;

public interface ISpotifyRemoteState
{
    ISpotifyPlayer Player { get; }
    Task<RequestResult> OnRequest(SpotifyWebsocketRequest request);
    Task<byte[]> UpdateState(
        PutStateReason reason,
        int playertime = -1);
}