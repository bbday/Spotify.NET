using System;
using SpotifyNET.Helpers;

namespace SpotifyNET.OneTimeStructures
{
    public readonly struct SpotifyConfig : IEquatable<SpotifyConfig>
    {
        public SpotifyConfig(string locale,
            string devicename = "Ongaku-Default")
        {
            DeviceName = devicename;
            Locale = locale;
            DeviceId = Utils.RandomHexString(40).ToLower();
        }


        public static SpotifyConfig Default => new SpotifyConfig("en");
        public string DeviceName { get; }
        public string Locale { get; }
        public string DeviceId { get; }

        public bool Equals(SpotifyConfig other)
        {
            return DeviceName == other.DeviceName && Locale == other.Locale && DeviceId == other.DeviceId;
        }

        public override bool Equals(object obj)
        {
            return obj is SpotifyConfig other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (DeviceName != null ? DeviceName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Locale != null ? Locale.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DeviceId != null ? DeviceId.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
