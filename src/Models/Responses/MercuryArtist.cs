using System.Text.Json.Serialization;

namespace SpotifyNET.Models.Responses
{
    public readonly struct MercuryArtist
    {
        [JsonConstructor]
        public MercuryArtist(SpotifyId uri, MercuryArtistInfoStruct info,
            MercuryArtistHeaderImageStruct header,
            MercuryArtistTopTracksStruct topTracks,
            MercuryArtistRelatedArtistsStruct relatedArtists,
            MercuryArtistBiographyStruct biography, MercuryArtistDiscographyStruct releases)
        {
            Uri = uri;
            Info = info;
            Header = header;
            TopTracks = topTracks;
            RelatedArtists = relatedArtists;
            Biography = biography;
            Releases = releases;
        }

        /// <summary>
        /// The spotify uri of the artist (Spotify:artist:....)
        /// </summary>
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Uri { get; }

        /// <summary>
        /// Containing metadata such as the name of the artist and portrait images.
        /// </summary>
        public MercuryArtistInfoStruct Info { get; }

        /// <summary>
        /// Contains the header image of the artist.
        /// </summary>
        [JsonPropertyName("header_image")]
        public MercuryArtistHeaderImageStruct Header { get; }

        [JsonPropertyName("top_tracks")]
        public MercuryArtistTopTracksStruct TopTracks { get; }
        [JsonPropertyName("related_artists")]
        public MercuryArtistRelatedArtistsStruct RelatedArtists { get; }

        public MercuryArtistBiographyStruct Biography { get; }

        public MercuryArtistDiscographyStruct Releases { get; }
    }

    public readonly struct MercuryArtistDiscographyStruct
    {
        [JsonConstructor]
        public MercuryArtistDiscographyStruct(MercuryArtistDiscographyItemStruct albums, MercuryArtistDiscographyItemStruct singles, MercuryArtistDiscographyItemStruct appearsOn, MercuryArtistDiscographyItemStruct compilations)
        {
            Albums = albums;
            Singles = singles;
            AppearsOn = appearsOn;
            Compilations = compilations;
        }

        public MercuryArtistDiscographyItemStruct Albums { get; }
        public MercuryArtistDiscographyItemStruct Singles { get; }
        [JsonPropertyName("appears_on")]
        public MercuryArtistDiscographyItemStruct AppearsOn { get; }
        public MercuryArtistDiscographyItemStruct Compilations { get; }
    }

    public readonly struct MercuryArtistDiscographyItemStruct
    {
        [JsonConstructor]
        public MercuryArtistDiscographyItemStruct(ushort count, MercuryArtistDiscographyRelease[] releases)
        {
            Count = count;
            Releases = releases;
        }

        [JsonPropertyName("total_count")]
        public ushort Count { get; }
        public MercuryArtistDiscographyRelease[]? Releases { get; }
    }

    public readonly struct MercuryArtistDiscographyRelease
    {
        [JsonConstructor]
        public MercuryArtistDiscographyRelease(SpotifyId uri, string name, UriImage cover, ushort year, ushort? month, ushort? day, MercuryArtistDiscographyItemDiscStruct[]? discs)
        {
            Uri = uri;
            Name = name;
            Cover = cover;
            Year = year;
            Month = month;
            Day = day;
            Discs = discs;
        }

        /// <summary>
        /// The spotify uri of the album (Spotify:album:....)
        /// </summary>
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Uri { get; }
        public string Name { get; }
        public UriImage Cover { get; }

        public ushort Year { get; }
        public ushort? Month { get; }
        public ushort? Day { get; }

        public MercuryArtistDiscographyItemDiscStruct[]? Discs { get; }
    }

    public readonly struct MercuryArtistDiscographyItemDiscStruct
    {
        [JsonConstructor]

        public MercuryArtistDiscographyItemDiscStruct(ushort number, MercuryArtistDiscographyItemDiscTrackStruct[] tracks)
        {
            Number = number;
            Tracks = tracks;
        }

        public ushort Number { get; }
        public MercuryArtistDiscographyItemDiscTrackStruct[] Tracks { get; }
    }

    public readonly struct MercuryArtistDiscographyItemDiscTrackStruct
    {
        [JsonConstructor]
        public MercuryArtistDiscographyItemDiscTrackStruct(SpotifyId uri, ulong? playcount, string name, ushort popularity, int duration, bool playable, bool @explicit)
        {
            Uri = uri;
            Playcount = playcount;
            Name = name;
            Popularity = popularity;
            Duration = duration;
            Playable = playable;
            Explicit = @explicit;
        }

        /// <summary>
        /// The spotify uri of the track (Spotify:track:....)
        /// </summary>
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Uri { get; }

        /// <summary>
        /// The playcount of the track. Null or 0 if no plays or LESS than 1000 plays
        /// </summary>
        public ulong? Playcount { get; }

        /// <summary>
        /// The name of the track
        /// </summary>
        public string Name { get; }

        public ushort Popularity { get; }

        public int Duration { get; }
        public bool Playable { get; }
        /// <summary>
        /// Boolean indicating if the track contains explicit language or content.
        /// </summary>
        public bool Explicit { get; }
    }
    public readonly struct MercuryArtistBiographyStruct
    {
        [JsonConstructor]
        public MercuryArtistBiographyStruct(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    public readonly struct MercuryArtistRelatedArtistsStruct
    {
        [JsonConstructor]
        public MercuryArtistRelatedArtistsStruct(MercuryArtistRelatedArtist[] artists)
        {
            Artists = artists;
        }

        /// <summary>
        /// An array containg the artists.
        /// </summary>
        public MercuryArtistRelatedArtist[] Artists { get; }
    }

    public readonly struct MercuryArtistRelatedArtist
    {
        [JsonConstructor]
        public MercuryArtistRelatedArtist(SpotifyId uri, string name, UriImage[] portraits)
        {
            Uri = uri;
            Name = name;
            Portraits = portraits;
        }

        /// <summary>
        /// The spotify uri of the artist (Spotify:artist:....)
        /// </summary>
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Uri { get; }

        /// <summary>
        /// the name of the artist.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The profile picture of the artist, as seen when searching for an artist, or on the home page of spotify.
        /// </summary>
        public UriImage[] Portraits { get; }
    }
    public readonly struct MercuryArtistInfoStruct
    {
        [JsonConstructor]
        public MercuryArtistInfoStruct(bool verified, string name, UriImage[] portraits)
        {
            Verified = verified;
            Name = name;
            Portraits = portraits;
        }

        /// <summary>
        /// The name of the artist
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The profile picture of the artist, as seen when searching for an artist, or on the home page of spotify.
        /// </summary>
        public UriImage[] Portraits { get; }

        /// <summary>
        /// True if the artist is verified.
        /// </summary>
        public bool Verified { get; }
    }

    public readonly struct MercuryArtistHeaderImageStruct
    {
        [JsonConstructor]
        public MercuryArtistHeaderImageStruct(string image)
        {
            Image = image;
        }

        /// <summary>
        /// The url of the image
        /// </summary>
        public string Image { get; }
    }

    public readonly struct MercuryArtistTopTracksStruct
    {
        [JsonConstructor]
        public MercuryArtistTopTracksStruct(MercuryArtistTopTrack[] tracks)
        {
            Tracks = tracks;
        }

        /// <summary>
        /// An array containing the top tracks for the artist in order as displayed on the spotify app.
        /// </summary>
        public MercuryArtistTopTrack[] Tracks { get; }
    }

    public readonly struct MercuryArtistTopTrack
    {
        [JsonConstructor]
        public MercuryArtistTopTrack(SpotifyId uri, string name, MercuryArtistTopTrackRelease release, bool @explicit, ulong? playcount)
        {
            Uri = uri;
            Name = name;
            Release = release;
            Explicit = @explicit;
            Playcount = playcount;
        }

        /// <summary>
        /// The spotify uri of the track (Spotify:track:....)
        /// </summary>
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Uri { get; }

        /// <summary>
        /// The playcount of the track. Null or 0 if no plays or LESS than 1000 plays
        /// </summary>
        public ulong? Playcount { get; }

        /// <summary>
        /// The name of the track
        /// </summary>
        public string Name { get; }

        //The album of the track.
        public MercuryArtistTopTrackRelease Release { get; }

        /// <summary>
        /// Boolean indicating if the track contains explicit language or content.
        /// </summary>
        public bool Explicit { get; }
    }

    public readonly struct MercuryArtistTopTrackRelease
    {
        [JsonConstructor]
        public MercuryArtistTopTrackRelease(SpotifyId uri, string name, UriImage cover)
        {
            Uri = uri;
            Name = name;
            Cover = cover;
        }

        /// <summary>
        /// The spotify uri of the album (Spotify:album:....)
        /// </summary>
        [JsonConverter(typeof(UriToSpotifyIdConverter))]
        public SpotifyId Uri { get; }
        public string Name { get; }
        public UriImage Cover { get; }
    }

}
