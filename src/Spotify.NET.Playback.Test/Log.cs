using System.Diagnostics;

namespace Spotify.NET.Playback.Test
{
    public static class LogHelper
    {
        public static void Log(string input)
        {
            Debug.WriteLine(input);
            Console.WriteLine(input);
        }
    }
}
