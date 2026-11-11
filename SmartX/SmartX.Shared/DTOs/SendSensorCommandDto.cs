
namespace SmartX.Shared.DTOs;

public sealed class SendSensorCommandDto
{
    public Guid SensorId { get; set; }

    public string Command { get; set; } = string.Empty;
}
