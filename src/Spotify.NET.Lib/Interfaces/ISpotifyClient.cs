#nullable enable
using System.Threading;
using System.Threading.Tasks;
using CPlayerLib;
using Google.Protobuf;
using SpotifyNET.Enums;
using SpotifyNET.Exceptions;
using SpotifyNET.Models;
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
        /// The country code returned by Spotify. This is used for many things such as determining availability of a track.
        /// Note: This cannot be used to get around region-locked content. As the spotify api will simply refuse to return playable urls.
        /// This is used so the client can avoid unnecesary api calls. 
        /// </summary>
        string? ReceivedCountryCode { get; }

        /// <summary>
        /// Async connect to Spotify and authenticates.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<APWelcome> ConnectAndAuthenticateAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets a generic bearer token that can be used to authenticate http/s endpoints to various spotify api endpoints.
        /// This also includes public api calls that can be found on the web-api.
        /// </summary>
        /// <param name="ct">A cancellation token for the asynchronous task.</param>
        /// <returns></returns>
        Task<MercuryToken> GetBearerAsync(CancellationToken ct = default);

        /// <summary>
        /// Send a request to a mercury endpoint and returns the response in json and deserializes it to <see cref="T"/>,
        /// or throws a <see cref="MercuryException"/>.
        ///
        /// NOTE: Not all mercury endpoints support json.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the json response to.</typeparam>
        /// <param name="mercuryUri">The mercury uri to send the request to.</param>
        /// <param name="type">The type of request. Default = GET</param>
        /// <param name="ct">A cancellation token for the asynchronous task.</param>
        /// <returns></returns>
        Task<T> SendAndReceiveAsJson<T>(
            string mercuryUri,
            MercuryRequestType type = MercuryRequestType.Get,
            CancellationToken ct = default);


        /// <summary>
        /// Send a request to a mercury endpoint and returns the as a raw <see cref="MercuryResponse"/>
        /// or throws a <see cref="MercuryException"/>.
        /// </summary>
        /// <param name="mercuryUri">The mercury uri to send the request to.</param>
        /// <param name="type">The type of request. Default = GET</param>
        /// <param name="ct">A cancellation token for the asynchronous task.</param>
        Task<MercuryResponse> SendAndReceiveAsMercuryResponse(
            string mercuryUri,
            MercuryRequestType type = MercuryRequestType.Get,
            CancellationToken ct = default);

        /// <summary>
        /// Gets the audio decrypt key for a file with the associated track.
        /// </summary>
        /// <param name="trackGid">The GID of the track.</param>
        /// <param name="preferredQualityFileId">The GID of the file.</param>
        /// <param name="ct">A cancellation token for the asynchronous task.</param>
        /// <returns></returns>
        Task<byte[]> GetAudioKeyAsync(ByteString trackGid,
            ByteString preferredQualityFileId, 
            bool retry = false,
            CancellationToken ct = default);
    }
}