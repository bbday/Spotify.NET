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

    ValueTask SendPackageAsync(
        MercuryPacket packet,
        CancellationToken ct = default);

    ValueTask<MercuryPacket> ReceivePackageAsync(
        CancellationToken ct);

    ValueTask<MercuryResponse?> SendAndReceiveAsResponse(string mercuryUri, MercuryRequestType type, CancellationToken ct = default);
}