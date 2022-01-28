using System;
using System.Text.Json.Serialization;

namespace SpotifyNET.Models
{
    public static class TimeHelper
    {
        internal static readonly DateTime Jan1St1970 = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillisSystem => (long)(DateTime.UtcNow - Jan1St1970).TotalMilliseconds;
    }
    public struct MercuryToken
    {

        [JsonConstructor]
        public MercuryToken(string accessToken, int expiresIn) =>
            (AccessToken, ExpiresIn, CreatedAt) = (accessToken, expiresIn, DateTime.UtcNow);

        [JsonPropertyName("accessToken")] 
        public string AccessToken { get; internal set; }
        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; }
        [JsonIgnore] public DateTime CreatedAt { get; }

        [JsonIgnore]
        public TimeSpan RemainingTime => CreatedAt.AddSeconds(ExpiresIn)
                                         - DateTime.UtcNow;

        public override string ToString() => RemainingTime.TotalMilliseconds > 0 ? AccessToken : "Expired";
    }

    public static class MercuryTokenExtensions
    {
        private const int TokenExpireThreshold = 10;

        public static bool IsExpired(this MercuryToken token) =>
            !(token.RemainingTime.TotalMilliseconds > 0);
    }
}