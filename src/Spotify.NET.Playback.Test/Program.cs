using System.Reflection;
using LibVLCSharp.Shared;
using Spotify.NET.Playback;
using Spotify.NET.Playback.Test;
using SpotifyNET;
using SpotifyNET.Enums;
using SpotifyNET.Helpers;
using SpotifyNET.Models;
using SpotifyNET.OneTimeStructures;
using static Spotify.NET.Playback.Test.LogHelper;
// Default installation path of VideoLAN.LibVLC.Windows
//var libDirectory =
//  new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

using var libvlc = new LibVLC(enableDebugLogs: true);
using var mediaplayer = new MediaPlayer(libvlc);

var pass = Environment.GetEnvironmentVariable("spotify_pass", EnvironmentVariableTarget.User);
var userpass = new UserPassAuthenticator("christos@marteco.nl", pass);

var client = new SpotifyClient(userpass, SpotifyConfig.Default);
var apWelcome = await client.ConnectAndAuthenticateAsync();

Console.WriteLine($"Welcome {apWelcome.CanonicalUsername}");

var connect = new SpotifyRemoteConnect(client, new ConsolePlayerTest(client));
SpotifyId? previousUri = null;
connect.ClusterUpdated += async (sender, cluster) =>
{
    if (cluster.PlayerState.Track != null)
    {
        var trackUri = new SpotifyId(cluster.PlayerState.Track.Uri);
        if (trackUri.IsValidId && !trackUri.Equals(previousUri))
        {
            switch (trackUri.Type)
            {
                case AudioItemType.Track:
                    var metadata = await client.GetMetadataForTrackAsync(trackUri);
                    Log($"Currently playing: {metadata.Name}");
                    break;
            }
        }
    }
};
await connect.ConnectAsync();

var t = "";
Console.ReadKey();
////https://open.spotify.com/track/3AUTwFhXWmB9Ty350sIlK2?si=68a12c0e0095464a
////https://open.spotify.com/track/6HN2uyyO6Wlbyp5qnooNpn?si=cec1f7496d534225
////https://open.spotify.com/track/3AUTwFhXWmB9Ty350sIlK2?si=a9595ad6203343a7
////https://open.spotify.com/track/0q5lnUuDhlogtYCOubNQhQ?si=155cf4f026de4bd5
//var 君という名の翼 = new SpotifyId("spotify:track:7jkrxM1JAK7BAjrKZY7EnD");
////
//var a = await client.StreamItemAsync(君という名の翼, AudioQualityExtensions.AudioQuality.HIGH);

//var p = new Media(libvlc, new StreamMediaInput(a));
//await p.Parse(MediaParseOptions.ParseNetwork);
//mediaplayer.Play(p);
//Console.ReadKey();
