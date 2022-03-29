using Spotify.NET.Playback;
using SpotifyNET;
using SpotifyNET.Models;
using SpotifyNET.OneTimeStructures;

var pass = Environment.GetEnvironmentVariable("spotify_pass", EnvironmentVariableTarget.User);
var userpass = new UserPassAuthenticator("tak123chris@gmail.com", pass);

var client = new SpotifyClient(userpass, SpotifyConfig.Default);
var apWelcome = await client.ConnectAndAuthenticateAsync();

Console.WriteLine($"Welcome {apWelcome.CanonicalUsername}");

//https://open.spotify.com/track/6HN2uyyO6Wlbyp5qnooNpn?si=cec1f7496d534225
var id = new SpotifyId("spotify:track:6HN2uyyO6Wlbyp5qnooNpn");

var a = await client.StreamItemAsync(id, AudioQualityExtensions.AudioQuality.HIGH);
//using var fs = File.Create("test.ogg");
//a.CopyTo(fs);
var a2 = "";