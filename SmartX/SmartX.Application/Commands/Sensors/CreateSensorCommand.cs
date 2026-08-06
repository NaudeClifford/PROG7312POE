using SmartX.Domain.Enums;

namespace SmartX.Application.Commands.Sensors;

public class CreateSensorCommand
{
    public string Name { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public SensorCategory Category { get; set; }

    public string Description { get; set; } = string.Empty;

    public Guid? GatewayId { get; set; }
}