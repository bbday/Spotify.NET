using System.Threading.Tasks;
using Connectstate;

namespace SpotifyNET.Interfaces;

public interface ISpotifyRemoteState
{
    Task<byte[]> UpdateState(
        PutStateReason reason, PlayerState st,
        int playertime = -1);
    PlayerState State { get; }
}