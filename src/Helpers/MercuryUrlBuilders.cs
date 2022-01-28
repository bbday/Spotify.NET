using SpotifyNET.Models;

namespace SpotifyNET.Helpers;

public static class MercuryUrlBuilders
{
    /// <summary>
    /// The response of a mercury request on this url, returns the artist data, found in the old spotify desktop app.
    /// It includes data such as the name, portraits, header images, related artists, top tracks & discography.
    /// </summary>
    /// <param name="id">The artist to fetch.</param>
    /// <param name="locale">The language. Defaults to english (en)</param>
    /// <returns></returns>
    public static string FetchArtistHomePage(SpotifyId id, string locale = "en")
        => $"hm://artist/v1/{id.Id}/desktop?format=json&catalogue=premium&locale={locale}&cat=1";

    /// <summary>
    /// The response of a mercury request on this url, returns extra metadata of the artist, such as biography,
    /// gallery images and cities.
    /// </summary>
    /// <param name="id">The artist to fetch.</param>
    /// <returns></returns>
    public static string FetchArtistInsights(SpotifyId id) =>
        $"hm://creatorabout/v0/artist-insights/{id.Id}";

    /// <summary>
    /// The response of a mercury request on this url, returns recommended albums for the current authenticated user.
    /// </summary>
    /// <returns></returns>
    public static string FetchAlbumRecommendations(int limit = 50) =>
        $"hm://your-library-view/v1/recommendations/albums?limit={limit}";

    
    /// <summary>
    /// The response of a mercury request on this url, returns data about an album. Returns the discs, images and related albums.
    /// </summary>
    /// <param name="uri">The album to fetch</param>
    /// <param name="country">The country, for copyright & playability.</param>
    /// <param name="locale">The language. Defaults to english (en)</param>
    /// <returns></returns>
    public static string FetchAlbumPage(SpotifyId uri,
        string country,
        string locale = "en")
        => $"hm://album/v1/album-app/album/{uri.Uri}/desktop?country={country}&catalogue=premium&locale={locale}";


    public static string Metadata(this SpotifyId id)
        => $"hm://metadata/4/{id.Type.ToString().ToLower()}/{id.ToHexId().ToLower()}";
}