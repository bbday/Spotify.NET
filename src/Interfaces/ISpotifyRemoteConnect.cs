using System;
using System.Threading;
using System.Threading.Tasks;
using Connectstate;

namespace SpotifyNET.Interfaces;

public interface ISpotifyRemoteConnect : IDisposable
{
    ISpotifyRemoteState SpotifyRemoteState { get; }
    string? ConnectionId { get; }
    event EventHandler<string> ConnectionIdUpdated;
    bool IsConnected { get; }
    Cluster CurrentCluster { get; }
    event EventHandler<Cluster> ClusterUpdated;
    Task<Cluster> ConnectAsync(CancellationToken ct = default);
}