using System.Threading;
using System.Threading.Tasks;
using CPlayerLib;
using Google.Protobuf;
using SpotifyNET.Interfaces;

namespace SpotifyNET.OneTimeStructures
{

    public readonly struct UserPassAuthenticator : IAuthenticator
    {
        private readonly string _username;
        private readonly ByteString _data;

        public UserPassAuthenticator(string username,
            string pwd)
        {
            _username = username;
            _data = ByteString.CopyFromUtf8(pwd);
        }

        public ValueTask<LoginCredentials> GetAsync(CancellationToken ct = default)
        {
            return new ValueTask<LoginCredentials>(new LoginCredentials
            {
                Username = _username,
                AuthData = _data,
                Typ = AuthenticationType.AuthenticationUserPass
            });
        }
    }
}