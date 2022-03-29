#nullable enable
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SpotifyNET.Enums;
using SpotifyNET.Models;

namespace SpotifyNET.Interfaces;

public interface ISpotifyTcpState : IDisposable
{   
    /// <summary>
    /// A boolean indicating whether or not the current instance of ISpotifyClient is connected to the spotify tcp connection.
    /// This does NOT check for an active websocket connection.
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// The main TCP Connection to spotify.
    /// </summary>
    TcpClient? TcpClient { get; }

    Task ConnectToTcpClient(CancellationToken ct = default);
    /// <summary>
    /// The country code returned by Spotify. This is used for many things such as determining availability of a track.
    /// Note: This cannot be used to get around region-locked content. As the spotify api will simply refuse to return playable urls.
    /// This is used so the client can avoid unnecesary api calls. 
    /// </summary>
    string? ReceivedCountryCode { get; }
    Task SendPackageAsync(
        MercuryPacket packet,
        CancellationToken ct = default);

    Task<MercuryPacket> ReceivePackageAsync(
        CancellationToken ct);

    Task<MercuryResponse?> SendAndReceiveAsResponse(string mercuryUri, MercuryRequestType type, CancellationToken ct = default);
}