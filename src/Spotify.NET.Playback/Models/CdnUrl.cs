using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Google.Protobuf;
using SpotifyNET.Helpers;
using SpotifyNET.Models;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}

namespace Spotify.NET.Playback.Models
{
    public static class CdnUrlExtensions
    {
        private static HttpClient _client;

        static CdnUrlExtensions()
        {
            _client = new HttpClient();
        }

        public static readonly int CHUNK_SIZE =  2 * 128 * 1024;

        public static async Task<(byte[] chunk, string? content_length)> ChunkRequest(this CdnUrl cdnUrl, ushort chunk_index,
            bool return_content_length = false,
            CancellationToken ct = default)
        {
            var range_start = chunk_index * CHUNK_SIZE;
            var range_end = (chunk_index + 1) * CHUNK_SIZE - 1;
            using (var requestMessage =
                   new HttpRequestMessage(HttpMethod.Get, cdnUrl.Url))
            {
                requestMessage.Headers.Range =
                    new RangeHeaderValue(range_start, range_end);
                var response =
                    await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, ct);
                if (response.StatusCode == (HttpStatusCode.RequestedRangeNotSatisfiable))
                {
                    throw new IndexOutOfRangeException("Invalid range.");
                }

                response.EnsureSuccessStatusCode();
                var streamToReadFrom = await response.Content.ReadAsByteArrayAsync();

                return (streamToReadFrom,
                    return_content_length
                        ? response.Content.Headers.First(a => a.Key == "Content-Range").Value.First() : null);
            }
        }
    }

    public readonly struct CdnUrl
    {
        public readonly Uri Url;
        private readonly ByteString _fileId;

        private readonly long _expirationMS;
        public CdnUrl(Uri url, ByteString fileId)
        {
            Url = url;
            _fileId = fileId;

            _expirationMS = -1;

            var token = HttpUtility.ParseQueryString(Url.Query)
                .Get("__token__");
            if (!string.IsNullOrEmpty(token))
            {
                var split_as_span = token.SplitLines('~');
                foreach (var lineSplit in split_as_span)
                {
                    int i = lineSplit.Line.IndexOf('=');
                    if (i == -1) continue;
                    if (lineSplit.Line.Slice(0, i).SequenceEqual(new char[]
                        {
                            'e',
                            'x',
                            'p'
                        }))
                    {
                        _expirationMS = long.Parse(lineSplit.Line.Slice(i + 1).ToString()) * 1000;
                    }
                }
            }
            else
            {
                int i = Url.Query.IndexOf('_');
                if (i != -1)
                {
                    _expirationMS = long.Parse(Url.Query.AsSpan().Slice(0, i).ToString().Replace("?", "")) * 1000;
                }
            }

            if (_expirationMS == -1)
            {
                throw new NotSupportedException();
            }
        }

        public bool IsExpired => TimeHelper.CurrentTimeMillisSystem > _expirationMS;
    }
}
