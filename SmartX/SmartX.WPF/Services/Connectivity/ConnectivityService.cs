using SmartX.WPF.Services.Connectivity;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;

namespace SmartX.WPF.Services;

public sealed class ConnectivityService :
    IConnectivityService,
    IDisposable
{
    private readonly HttpClient _httpClient;

    public ConnectivityService(
        HttpClient httpClient)
    {
        _httpClient = httpClient;

        IsOnline =
            NetworkInterface.GetIsNetworkAvailable();

        NetworkChange.NetworkAvailabilityChanged +=
            OnNetworkAvailabilityChanged;
    }

    public bool IsOnline { get; private set; }

    public event EventHandler<bool>?
        NetworkAvailabilityChanged;

    public Task<bool> IsNetworkAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var available =
            NetworkInterface.GetIsNetworkAvailable();

        IsOnline = available;

        return Task.FromResult(available);
    }

    public async Task<bool> CheckConnectivityAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response =
                await _httpClient.GetAsync(
                    "api/health",
                    cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine(ex);

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);

            return false;
        }
    }

    private void OnNetworkAvailabilityChanged(
        object? sender,
        NetworkAvailabilityEventArgs e)
    {
        IsOnline = e.IsAvailable;

        NetworkAvailabilityChanged?.Invoke(
            this,
            e.IsAvailable);
    }

    public void Dispose()
    {
        NetworkChange.NetworkAvailabilityChanged -=
            OnNetworkAvailabilityChanged;
    }
}
