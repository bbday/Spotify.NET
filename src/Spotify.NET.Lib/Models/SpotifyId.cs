﻿using System;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Base62;
using Google.Protobuf;
using SpotifyNET.Enums;
using SpotifyNET.Helpers;

namespace SpotifyNET.Models
{
    public enum PlayType
    {
        spotify,
        local
    }
    public readonly struct SpotifyId : IEquatable<SpotifyId>, IComparable<SpotifyId>
    {
        public static SpotifyId With(string id, AudioItemType type) =>
            new SpotifyId($"spotify:{type.ToString().ToLower()}:{id}");
        
        public SpotifyId WithType(AudioItemType type) => new SpotifyId(
            $"spotify:{type.ToString().ToLower()}:{Id}");

        [JsonConstructor]
        public SpotifyId(string uri)
        {
            IsValidId = false;
            Id = null;
            Uri = uri;
            Source = PlayType.local;
            Type = AudioItemType.Unknown;
            var s =
                uri.SplitLines();

            var i = 0;
            while (s.MoveNext())
            {
                switch (i)
                {
                    case 0:
                        switch (s.Current.Line.ToString())
                        {
                            case nameof(PlayType.local):
                                Source = PlayType.local;
                                break;
                            case nameof(PlayType.spotify):
                                Source = PlayType.spotify;
                                break;
                        }
                        i++;
                        break;
                    case 1:
                        Type = GetType(s.Current.Line, uri);
                        i++;
                        break;
                    case 2:
                        Id = s.Current.Line.ToString();
                        i++;
                        IsValidId = true;
                        break;
                    default:
                        break;
                }
            }
        }
        public bool IsValidId { get; }
        public PlayType Source { get; }
        public string Uri { get; }
        public AudioItemType Type { get; }
        public string Id { get; }
      

        public override int GetHashCode()
        {
            return (Uri != null ? Uri.GetHashCode() : 0);
        }

        private static AudioItemType GetType(ReadOnlySpan<char> r,
           string uri)
        {
            switch (r)
            {
                case var start_group when
                    start_group.SequenceEqual("start-group".AsSpan()):
                    return AudioItemType.StartGroup;
                case var end_group when
                    end_group.SequenceEqual("end-group".AsSpan()):
                    return AudioItemType.EndGroup;
                case var end_group when
                    end_group.SequenceEqual("station".AsSpan()):
                    return AudioItemType.Station;
                case var track when
                    track.SequenceEqual("track".AsSpan()):
                    return AudioItemType.Track;
                case var artist when
                    artist.SequenceEqual("artist".AsSpan()):
                    return AudioItemType.Artist;
                case var album when
                    album.SequenceEqual("album".AsSpan()):
                    return AudioItemType.Album;
                case var show when
                    show.SequenceEqual("show".AsSpan()):
                    return AudioItemType.Show;
                case var episode when
                    episode.SequenceEqual("episode".AsSpan()):
                    return AudioItemType.Episode;
                case var playlist when
                    playlist.SequenceEqual("playlist".AsSpan()):
                    return AudioItemType.Playlist;
                case var collection when
                    collection.SequenceEqual("collection".AsSpan()):
                    return AudioItemType.Link;
                case var app when
                    app.SequenceEqual("app".AsSpan()):
                    return AudioItemType.Link;
                case var dailymixhub when
                    dailymixhub.SequenceEqual("daily-mix-hub".AsSpan()):
                    return AudioItemType.Link;
                case var user when
                    user.SequenceEqual("daily-mix-hub".AsSpan()):
                    {
                        var regexMatch = Regex.Match(uri, "spotify:user:(.*):playlist:(.{22})");
                        if (regexMatch.Success)
                        {
                            return AudioItemType.Playlist;
                        }

                        regexMatch = Regex.Match(uri, "spotify:user:(.*):collection");
                        return regexMatch.Success ? AudioItemType.Link : AudioItemType.User;
                    }
                default:
                    return AudioItemType.Link;
            }
        }

        public int Compare(SpotifyId x, SpotifyId y)
        {
            return string.Compare(x.Uri, y.Uri, StringComparison.Ordinal);
        }

        public int CompareTo(SpotifyId other)
        {
            return string.Compare(Uri, other.Uri, StringComparison.Ordinal);
        }

        public static SpotifyId FromHex(string hex, AudioItemType type)
        {
            var k = Base62Test.Encode(Utils.HexToBytes(hex));
            var j = $"spotify:{type.ToString().ToLower()}:" + Encoding.Default.GetString(k);
            return new SpotifyId(j);
        }
        public string ToHex()
        {
            var k = Id.FromBase62();
            return k.BytesToHex().ToLowerInvariant();
        }
        private static readonly Base62Test Base62Test
            = Base62Test.CreateInstanceWithInvertedCharacterSet();

        public static SpotifyId FromGid(ByteString albumGid, AudioItemType album)
        {
            return SpotifyId.FromHex(albumGid.ToByteArray().BytesToHex(), album);
        }

        public bool Equals(SpotifyId other)
        {
            return Uri == other.Uri;
        }

        public override bool Equals(object obj)
        {
            return obj is SpotifyId other && Equals(other);
        }
    }
}