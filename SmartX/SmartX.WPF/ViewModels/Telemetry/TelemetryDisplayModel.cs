namespace SmartX.WPF.ViewModels.Telemetry;

public class TelemetryDisplayModel
{
    public Guid Id { get; init; }

    public Guid SensorId { get; init; }

    public string SensorName { get; init; } = "Unknown Sensor";

    public DateTime Timestamp { get; init; }

    public double? Voltage { get; init; }

    public double? Current { get; init; }

    public double? Power { get; init; }

    public double? Temperature { get; init; }
}
