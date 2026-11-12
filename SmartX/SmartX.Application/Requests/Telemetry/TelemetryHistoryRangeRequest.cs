namespace SmartX.Application.Requests.Telemetry;

public class TelemetryHistoryRangeRequest
{
    public Guid SensorId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}