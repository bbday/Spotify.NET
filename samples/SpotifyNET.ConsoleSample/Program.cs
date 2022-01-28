// See https://aka.ms/new-console-template for more information
//$Q}?R./d:NA;;3s,


using System.Diagnostics;
using System.Runtime.InteropServices;
using CPlayerLib;
using Spotify.Metadata;
using SpotifyNET;
using SpotifyNET.Enums;
using SpotifyNET.Helpers;
using SpotifyNET.Models;
using SpotifyNET.OneTimeStructures;


var pass = Environment.GetEnvironmentVariable("spotify_pass", EnvironmentVariableTarget.User);
var userpass = new UserPassAuthenticator("christos@marteco.nl", pass);

var client = new SpotifyClient(userpass, SpotifyConfig.Default);
var t = Stopwatch.StartNew();
var ap = await client.ConnectAndAuthenticateAsync();
t.Stop();
var e = t.ElapsedMilliseconds;

var token = await client.GetBearerAsync();
Console.WriteLine(token.AccessToken);


var metadataurl = SpotifyId.With("1GxkXlMwML1oSg5eLPiAz3", AudioItemType.Artist)
    .Metadata();

var metadata = await client.TcpState.
    SendAndReceiveAsResponse(metadataurl, MercuryRequestType.Get);
var track = Artist.Parser.ParseFrom(metadata.Value.Payload.SelectMany(a => a).ToArray());

var k = "";
