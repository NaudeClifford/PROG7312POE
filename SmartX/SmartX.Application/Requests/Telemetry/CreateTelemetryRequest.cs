namespace SmartX.Application.Requests.Telemetry;

public class CreateTelemetryRequest
{
    public Guid SensorId { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public double? Voltage { get; set; }

    public double? Current { get; set; }

    public double? Power { get; set; }

    public double? Temperature { get; set; }
}
