using System.Threading.Tasks;
using Connectstate;
using SpotifyNET.Enums;
using SpotifyNET.Models;

namespace SpotifyNET.Interfaces;

public interface ISpotifyPlayer
{  
    /// <summary>
    /// The state of the player. This is initially set by the library.
    /// But the developer should keep it up the date.
    /// </summary>
    PlayerState State { get; set; }

    /// <summary>
    /// The state of the player. This is initially set by the library.
    /// But the developer should keep it up the date.
    /// </summary>
    PutStateRequest PutState { get; set; }
    Task<RequestResult> IncomingCommand(Endpoint endpoint, CommandBody? data);
}