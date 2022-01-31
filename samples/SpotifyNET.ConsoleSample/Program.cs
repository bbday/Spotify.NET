// See https://aka.ms/new-console-template for more information
//$Q}?R./d:NA;;3s,


using System.Diagnostics;
using System.Runtime.InteropServices;
using CPlayerLib;
using Spotify.Metadata;
using SpotifyNET;
using SpotifyNET.Enums;
using SpotifyNET.Helpers;
using SpotifyNET.Interfaces;
using SpotifyNET.Models;
using SpotifyNET.OneTimeStructures;


var pass = Environment.GetEnvironmentVariable("spotify_pass", EnvironmentVariableTarget.User);
var userpass = new UserPassAuthenticator("christos@marteco.nl", pass);

var client = new SpotifyClient(userpass, SpotifyConfig.Default);
var apWelcome = await client.ConnectAndAuthenticateAsync();

Console.WriteLine($"Welcome {apWelcome.CanonicalUsername}");

ISpotifyRemoteConnect connect = new SpotifyRemoteConnect(client);
var cluster = await connect.ConnectAsync();


connect.ClusterUpdated += (sender, cluster1) =>
{
Console.WriteLine(cluster1);
};

var m = new ManualResetEvent(false);
m.WaitOne();

var token =await client.GetBearerAsync();

Console.WriteLine($"Bearer: \r\n {token.AccessToken}");

var michaelBubleMetadataUrl = SpotifyId.With("1GxkXlMwML1oSg5eLPiAz3", AudioItemType.Artist)
    .Metadata();

var michaelBubleMetadataResponse = await client.TcpState!.
    SendAndReceiveAsResponse(michaelBubleMetadataUrl, MercuryRequestType.Get);

var michaelBubleMetadata = Artist.Parser.ParseFrom(michaelBubleMetadataResponse.Value.Payload.SelectMany(a => a).ToArray());

Console.WriteLine(michaelBubleMetadata);
