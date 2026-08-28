namespace SmartX.Application.Commands.Telemetry;

public class CreateTelemetryCommand
{
    public Guid SensorId { get; set; }

    public DateTime TimeStamp { get; set; }

    public double? Voltage { get; set; }

    public double? Current { get; set; }

    public double? Power { get; set; }

    public double? Temperature { get; set; }
}
