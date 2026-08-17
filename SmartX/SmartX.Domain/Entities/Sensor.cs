using SmartX.Domain.Enums;

namespace SmartX.Domain.Entities;

public class Sensor
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;

    public SensorCategory Category { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Guid? GatewayId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}