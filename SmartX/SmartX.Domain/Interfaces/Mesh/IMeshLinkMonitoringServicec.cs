namespace SmartX.Domain.Interfaces.Mesh;

public interface IMeshLinkMonitoringService
{
    Task EvaluateGatewayLinksAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);
}