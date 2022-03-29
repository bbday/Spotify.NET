using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CPlayerLib.Download.Proto;
using Flurl;
using Flurl.Http;
using Nito.AsyncEx;
using Spotify.Metadata;
using Spotify.NET.Playback.Models;
using SpotifyNET;
using SpotifyNET.Enums;
using SpotifyNET.Exceptions;
using SpotifyNET.Helpers;
using SpotifyNET.Interfaces;
using SpotifyNET.Models;

namespace Spotify.NET.Playback
{
    public static class PlaybackHelpers
    {

        private const string STORAGE_RESOLVE_INTERACTIVE = "/storage-resolve/files/audio/interactive";
        private const string STORAGE_RESOLVE_INTERACTIVE_PREFETCH = "/storage-resolve/files/audio/interactive_prefetch";

        public static async Task<SpotifyStream> StreamItemAsync(this SpotifyClient client, SpotifyId id,
            AudioQualityExtensions.AudioQuality quality,
            bool preload = false,
            CancellationToken ct = default)
        {
            switch (id.Type)
            {
                case AudioItemType.Track:
                    var original = await client.GetMetadataForTrackAsync(id, ct);
                    return await StreamTrackAsync(client, original, quality, preload, ct);
                case AudioItemType.Episode:
                    var episode = await client.GetMetadataForEpisodeAsync(id, ct);
                    break;
                default:
                    throw new NotSupportedException("Cannot stream anything other than track or episode.");
            }

            throw new AbandonedMutexException();
        }

        public static async Task<SpotifyStream> StreamTrackAsync(this ISpotifyClient client, Track track,
            AudioQualityExtensions.AudioQuality quality,
            bool preload = false,
            CancellationToken ct = default)
        {
            if (track.File.Count == 0)
            {
                track.File.AddRange(track.Alternative.SelectMany(a => a.File));
            }

            var country = client.ReceivedCountryCode;

            if (country != null)
            {
                ContentRestrictedException.CheckRestrictions(country, track.Restriction);
            }

            static bool IsVorbis(AudioFile.Types.Format format)
            {
                switch (format)
                {
                    case AudioFile.Types.Format.OggVorbis96:
                    case AudioFile.Types.Format.OggVorbis160:
                    case AudioFile.Types.Format.OggVorbis320:
                        return true;
                    default:
                        return false;
                }
            }

            //if country is null, we just continue and the spotify api will tell us if the content is unavailable.
            var picked = track.File
                .Where(a => a.HasFormat &&
                            IsVorbis(a.Format))?.ToImmutableArray();
            var preferredQuality =
                picked?.FirstOrDefault(a => AudioQualityExtensions.GetQuality(a.Format) == quality) ??
                picked?.FirstOrDefault();
            if (preferredQuality == null)
                throw new NotSupportedException("Could not find any vorbis files.");

            var spclient = await ApResolver.GetClosestSpClient(ct);

            using var resp = await spclient
                .AppendPathSegment(preload ? STORAGE_RESOLVE_INTERACTIVE_PREFETCH : STORAGE_RESOLVE_INTERACTIVE)
                .AppendPathSegments(Utils.BytesToHex(preferredQuality.FileId))
                .WithOAuthBearerToken((await client.GetBearerAsync(ct)).AccessToken)
                .GetStreamAsync(cancellationToken: ct);

            var storageResolveResponse = StorageResolveResponse.Parser.ParseFrom(resp);
            switch (storageResolveResponse.Result)
            {
                case StorageResolveResponse.Types.Result.Cdn:
                    var start = Stopwatch.StartNew();
                    var key = await client.GetAudioKeyAsync(track.Gid, preferredQuality.FileId, true, ct);
                    var audioKeyItem = start.ElapsedMilliseconds;

                    var cdnUrl = new CdnUrl(new Uri(storageResolveResponse.Cdnurl.First()), preferredQuality.FileId);

                    var (chunk, contentLength) = await cdnUrl.ChunkRequest(0, true, ct);
                    var (size, chunks) = GetData(contentLength);

                    var str =
                        new SpotifyStream(chunk, chunks, size, key, cdnUrl);
                    if (str.Seek(0xa7, SeekOrigin.Begin) != 0xa7)
                        Debugger.Break();
                    return str;
                case StorageResolveResponse.Types.Result.Storage:
                    //TODO: What is this?
                    Debugger.Break();
                    break;
                case StorageResolveResponse.Types.Result.Restricted:
                    //TODO: What is this?
                    Debugger.Break();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }

        private static (ulong size, int chunks) GetData(string content_length)
        {
            var split = content_length.SplitLines('/');
            split.MoveNext();
            split.MoveNext();
            var size = ulong.Parse(split.Current.Line.ToString());
            return (size, (int)Math.Ceiling((float)size / (float)CdnUrlExtensions.CHUNK_SIZE));
        }
    }


    public static class AudioQualityExtensions
    {
        public enum AudioQuality
        {
            NORMAL,
            HIGH,
            VERY_HIGH
        }

        public static AudioQuality GetQuality(AudioFile.Types.Format format)
        {
            switch (format)
            {
                case AudioFile.Types.Format.Mp396:
                case AudioFile.Types.Format.OggVorbis96:
                    // TODO: AAC_24_NORM ????
                    return AudioQuality.NORMAL;
                case AudioFile.Types.Format.Mp3160:
                case AudioFile.Types.Format.Mp3160Enc:
                case AudioFile.Types.Format.OggVorbis160:
                case AudioFile.Types.Format.Aac24:
                    return AudioQuality.HIGH;
                case AudioFile.Types.Format.Mp3320:
                case AudioFile.Types.Format.Mp3256:
                case AudioFile.Types.Format.OggVorbis320:
                case AudioFile.Types.Format.Aac48:
                    return AudioQuality.VERY_HIGH;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), $"Could not quality for find format {format}");
            }
        }
    }
    public class SpotifyStream : Stream
    {
        public byte[][] _buffer;
        private bool[] _requested;
        private bool[] _available;
        private readonly AudioDecrypt _audioDecrypt;
        private readonly CdnUrl _cdnUrl;
        public SpotifyStream(byte[] first_chunk, int total_chunks,
            ulong total_size,
            byte[] audio_key, CdnUrl cdnUrl)
        {
            _cdnUrl = cdnUrl;
            _audioDecrypt = new AudioDecrypt(audio_key);
            Length = (long)total_size;
            _buffer = new byte[total_chunks][];
            _requested = new bool[total_chunks];
            _requested[0] = true;
            _available = new bool[total_chunks];
            GetChunkAsync(0, first_chunk).Wait();
        }
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0) return 0;
            if (pos >= Length) return -1; 
            //while (true)
            //{
            try
            {
                int chunk = pos / CdnUrlExtensions.CHUNK_SIZE;
                int chunkOff = pos % CdnUrlExtensions.CHUNK_SIZE;

                //AsyncContext.Run(async () => await GetChunkAsync((ushort) chunk));

                if(!_available[chunk]) 
                    AsyncContext.Run(async () => await GetChunkAsync((ushort)chunk));
                int copy = Math.Min(_buffer[chunk].Length - chunkOff, count);
                Array.Copy(_buffer[chunk],
                    chunkOff, buffer, offset, copy);
                pos += copy;

                return copy;
                //if (i == count || Position >= Length)
            }
            catch (Exception x)
            {
                Debug.WriteLine(x.ToString());
            }
            //}
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position += offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }
            return Position;
        }

        public override void SetLength(long value)
        {

        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length { get; }
        public override long Position
        {
            get => pos;
            set => pos = (int)value;
        }
        private int pos;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private async Task GetChunkAsync(ushort index, byte[]? chunk = null, CancellationToken ct = default)
        {
            _requested[index] = true;
            var get = chunk ??
                      (await _cdnUrl.ChunkRequest(index, ct: ct)).chunk;
            _audioDecrypt.DecryptChunk(index, get);
            Debug.WriteLine($"Chunk {index + 1}/{_available.Length} completed," +
                            $" stream: {ToString()}");
            Console.WriteLine($"Chunk {index + 1}/{_available.Length} completed," +
                            $" stream: {ToString()}");
            _available[index] = true;
            _buffer[index] = get;
        }
    }
}
