namespace SmartX.WPF.Services.Connectivity;

public interface IConnectivityService
{
    bool IsOnline { get; }

    Task<bool> CheckConnectivityAsync(
        CancellationToken cancellationToken = default);
}
