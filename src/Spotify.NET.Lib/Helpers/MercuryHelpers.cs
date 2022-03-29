using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spotify.Metadata;
using SpotifyNET.Models;

namespace SpotifyNET.Helpers
{
    public static class MercuryHelpers
    {
        public const string MetadataUrl = "hm://metadata/4/{0}/{1}";


        public static async Task<Track> GetMetadataForTrackAsync(this SpotifyClient client, SpotifyId id,
            CancellationToken ct = default)
        {
            var resp = await
                client.SendAndReceiveAsMercuryResponse(string.Format(MetadataUrl, "track", id.ToHexId().ToLower()), ct: ct);

            return Track.Parser.ParseFrom(resp.Payload.SelectMany(a => a).ToArray());
        }
        public static async Task<Episode> GetMetadataForEpisodeAsync(this SpotifyClient client, SpotifyId id,
            CancellationToken ct = default)
        {
            var resp = await
                client.SendAndReceiveAsMercuryResponse(string.Format(MetadataUrl, "episode", id.ToHex()), ct: ct);

            return Episode.Parser.ParseFrom(resp.Payload.SelectMany(a => a).ToArray());
        }
    }
}
