using System.Text.Json;
using Connectstate;
using SpotifyNET.Enums;
using SpotifyNET.Models;

namespace SpotifyNET.Interfaces;

public interface ISpotifyPlayer
{  
    PlayerState State { get; set; }
    PutStateRequest PutState { get; set; }
    RequestResult IncomingCommand(Endpoint endpoint, CommandBody? data);
}