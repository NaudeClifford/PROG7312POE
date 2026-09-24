namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryByGatewayQuery
{
    public Guid GatewayId { get; }

    public GetTelemetryByGatewayQuery(
        Guid gatewayId)
    {
        GatewayId = gatewayId;
    }
}
