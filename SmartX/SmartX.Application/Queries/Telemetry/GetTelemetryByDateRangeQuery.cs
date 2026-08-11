namespace SmartX.Application.Queries.Telemetry;

public class GetTelemetryByDateRangeQuery
{
    public Guid SensorId { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}