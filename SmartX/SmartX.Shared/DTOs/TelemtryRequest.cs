namespace SmartX.Shared.DTOs;

public class TelemetryRequest
{
    public string SensorId { get; set; } = string.Empty;

    public double Voltage { get; set; }

    public double Current { get; set; }

    public double Temperature { get; set; }
}