using System.Threading;
using System.Threading.Tasks;
using CPlayerLib;

namespace SpotifyNET.Interfaces
{
    public interface IAuthenticator 
    {
        ValueTask<LoginCredentials> GetAsync(CancellationToken ct = default);
    }
}
