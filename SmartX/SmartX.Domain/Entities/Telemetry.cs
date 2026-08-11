namespace SmartX.Domain.Entities;

public class Telemetry
{
    public Guid Id { get; set; }

    public Guid SensorId { get; set; }

    public DateTime Timestamp { get; set; }

    public double? Voltage { get; set; }

    public double? Current { get; set; }

    public double? Power { get; set; }

    public double? Temperature { get; set; }


}