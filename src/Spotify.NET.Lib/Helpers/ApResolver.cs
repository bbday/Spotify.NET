using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SpotifyNET.Helpers;

public static class ApResolver
{
    private static HttpClient _httpClient = new HttpClient();
    private static string _resolvedSpClient;

    public static async Task<(string, ushort)[]> GetClosestAccessPoint(CancellationToken ct)
    {
        using var test = await 
            _httpClient.GetStreamAsync("http://apresolve.spotify.com/?type=accesspoint");
        var spClients =
            await JsonSerializer.DeserializeAsync<AccessPoints>(test, cancellationToken: ct);
        return spClients.accesspoint.Select(host =>
            (host.Split(':')[0], ushort.Parse(host.Split(':')[1])))
            .ToArray();
    }
    private readonly struct AccessPoints
    {
        [JsonConstructor]
        public AccessPoints(string[] accesspoint)
        {
            this.accesspoint = accesspoint;
        }

        public string[] accesspoint { get; }
    }

    public static async Task<string> GetClosestDealerAsync(CancellationToken ct)
    {
        using var test = await 
            _httpClient.GetStreamAsync("http://apresolve.spotify.com/?type=dealer");
        var dealers =
            await JsonSerializer.DeserializeAsync<Dealers>(test, cancellationToken: ct);
        return "https://" +  dealers.dealer.FirstOrDefault();
    }
    private readonly struct Dealers
    {
        [JsonConstructor]
        public Dealers(string[] dealer)
        {
            this.dealer = dealer;
        }

        public string[] dealer { get; }
    }

    public static async Task<string> GetClosestSpClient(CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(_resolvedSpClient))
            return _resolvedSpClient;
        
        using var test = await 
            _httpClient.GetStreamAsync("http://apresolve.spotify.com/?type=spclient");
        var spclients =
            await JsonSerializer.DeserializeAsync<SpClients>(test, cancellationToken: ct);
        return "https://" +  spclients.spclient.FirstOrDefault();
    }
    
    private readonly struct SpClients
    {
        [JsonConstructor]
        public SpClients(string[] spclient)
        {
            this.spclient = spclient;
        }

        public string[] spclient { get; }
    }
}