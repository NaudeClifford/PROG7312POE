namespace SmartX.Shared.DTOs;

public class TelemetryRequest
{
    public Guid Id { get; set; }

    public Guid SensorId { get; set; }

    public DateTime Timestamp { get; set; }

    public double? Voltage { get; set; }

    public double? Current { get; set; }

    public double? Power { get; set; }

    public double? Temperature { get; set; }

    public DateTime CreatedAt { get; set; }
}