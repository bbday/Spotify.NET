#nullable enable
using System.Threading;
using System.Threading.Tasks;
using CPlayerLib;
using SpotifyNET.OneTimeStructures;

namespace SpotifyNET.Interfaces
{
    public interface ISpotifyClient
    {
        
        /// <summary>
        /// The active TCP Connection to Spotify.
        /// </summary>
        ISpotifyTcpState? TcpState { get; }
        
        /// <summary>
        /// A boolean indicating whether or not the current instance of ISpotifyClient is connected to the spotify tcp connection.
        /// This does NOT check for an active websocket connection.
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// The config for the spotify connection. Cannot be changed mid connection, only when reconnecting.
        /// </summary>
        SpotifyConfig Config { get; }
        
        /// <summary>
        /// Async connect to Spotify and authenticates.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<APWelcome> ConnectAndAuthenticateAsync(CancellationToken ct = default);
    }
}