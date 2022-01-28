// See https://aka.ms/new-console-template for more information
//$Q}?R./d:NA;;3s,


using SpotifyNET;
using SpotifyNET.OneTimeStructures;


var pass = Environment.GetEnvironmentVariable("spotify_pass", EnvironmentVariableTarget.User);
var userpass = new UserPassAuthenticator("christos@marteco.nl", pass);

var client = new SpotifyClient(userpass, SpotifyConfig.Default);
var ap = await client.ConnectAndAuthenticateAsync();
Console.WriteLine("Hello, World!");