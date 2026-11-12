namespace SmartX.Application.Interfaces.Mesh;

public interface IMeshMonitoringService
{
    Task RefreshAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task CheckNodeStatusAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task RecalculateRoutesAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}