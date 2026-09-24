namespace SmartX.WPF.ViewModels.History;

public sealed class TelemetryHistoryItem
{
    public Guid Id { get; init; }

    public Guid SensorId { get; init; }

    public string SensorName { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; }

    public double? Voltage { get; init; }

    public double? Current { get; init; }

    public double? Power { get; init; }

    public double? Temperature { get; init; }

    public string TimestampDisplay =>
        Timestamp.ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss");
}
 