namespace SmartX.Application.Commands.Sensors;

public class CreateSensorCommand
{
    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? GatewayId { get; set; }
}