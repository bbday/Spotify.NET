using System;
using System.Diagnostics;
using CPlayerLib;

namespace SpotifyNET.Exceptions
{
    public class SpotifyConnectionException : Exception
    {
        public SpotifyConnectionException(APResponseMessage responseMessage) :
            base(responseMessage.ToString())
        {
            Debug.WriteLine(responseMessage.ToString());
            LoginFailed = responseMessage;
        }

        public APResponseMessage LoginFailed { get; }
    }
}