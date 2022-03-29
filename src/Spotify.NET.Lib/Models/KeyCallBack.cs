using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace SpotifyNET.Models
{
    public class KeyCallBack 
    {
        public static readonly long AudioKeyRequestTimeout = 5000;
        private byte[] _reference;
        private readonly AsyncManualResetEvent _referenceLock = new AsyncManualResetEvent(false);

        public void Key(byte[] key)
        {
            _reference = key;
            _referenceLock.Set();
            _referenceLock.Reset();
        }

        public void Error(short code)
        {
            Debug.WriteLine("Audio key error, code: {0}", code);
            _reference = null;
            _referenceLock.Set();
            _referenceLock.Reset();
        }

        public async Task<byte[]> WaitResponseAsync(CancellationToken ct = default)
        {
            //TimeSpan.FromMilliseconds(AudioKeyRequestTimeout)
            await _referenceLock.WaitAsync(ct);
            return _reference;
        }
    }
}