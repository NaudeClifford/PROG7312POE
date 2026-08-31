using SmartX.WPF.Services.Connectivity;
using System.Diagnostics;
using System.Net.Http;

namespace SmartX.WPF.Services;

public sealed class ConnectivityService : IConnectivityService
{
    private readonly HttpClient _httpClient;

    public ConnectivityService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool IsOnline { get; private set; }

    public async Task<bool> CheckConnectivityAsync(
    CancellationToken cancellationToken = default)
    {
        try
        {

            using var response = await _httpClient.GetAsync(
                "api/health",
                cancellationToken);

            IsOnline = response.IsSuccessStatusCode;
            return IsOnline;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            throw;
        }
    }

}
