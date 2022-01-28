using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using SpotifyNET.Interfaces;

namespace SpotifyNET.Helpers
{
    internal class LoggingHandler : DelegatingHandler
    {
        private readonly ISpotifyClient _tokensProvider;

        internal LoggingHandler(HttpClientHandler innerHandler,
            ISpotifyClient tokensProvider) : base(innerHandler)
        {
            _tokensProvider = tokensProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer",
                    (await _tokensProvider.GetBearerAsync((cancellationToken))).AccessToken);

            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }
    }
}