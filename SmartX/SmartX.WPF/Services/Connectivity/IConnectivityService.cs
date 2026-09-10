namespace SmartX.WPF.Services.Connectivity;

public interface IConnectivityService
{
    bool IsOnline { get; }

    event EventHandler<bool>? NetworkAvailabilityChanged;

    Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken = default);

    Task<bool> IsNetworkAvailableAsync(CancellationToken cancellationToken = default);
}
