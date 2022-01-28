using System;
using SpotifyNET.Enums;

namespace SpotifyNET.Exceptions
{
    public class NoAccessPointFoundException : Exception
    {
        public NoAccessPointFoundException(NoAccessPointFoundReasonType reason, Exception? innerException) : base(reason.ToString(), innerException)
        {
            Reason = reason;
        }
        public NoAccessPointFoundReasonType Reason { get; }
    }
}
