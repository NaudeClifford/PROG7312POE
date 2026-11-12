namespace SmartX.Application.Interfaces.Mesh;

public interface IMeshLinkQualityService
{
    Task EvaluateGatewayLinksAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default);

    Task EvaluateLinkAsync(
        Guid linkId,
        CancellationToken cancellationToken = default);

    double CalculateLinkQuality(
        double? signalStrengthDbm,
        double? latencyMilliseconds,
        double? energyCost);

    double CalculateRoutingCost(
        double linkQuality,
        double latencyMilliseconds,
        double energyCost);
}