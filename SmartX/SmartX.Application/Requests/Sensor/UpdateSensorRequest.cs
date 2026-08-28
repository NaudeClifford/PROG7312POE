using SmartX.Domain.Enums;

namespace SmartX.Application.Requests.Sensor;

public class UpdateSensorRequest
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;

    public SensorCategory Category { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? GatewayId { get; set; }

    public bool IsActive { get; set; }
}
