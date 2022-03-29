using System;

namespace SpotifyNET.Exceptions
{
    public class AesKeyException : Exception
    {
        public AesKeyException(string message) : base(message)
        {

        }
    }
}
