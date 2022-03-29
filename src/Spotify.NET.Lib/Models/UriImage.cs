using System.Text.Json.Serialization;

namespace SpotifyNET.Models
{
    public readonly struct UriImage
    {
        [JsonConstructor]
        public UriImage(string uri)
        {
            Uri = uri;
        }

        public string Uri { get; }
    }
}