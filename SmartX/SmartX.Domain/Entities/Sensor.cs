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

    public string? IpAddress { get; set; }

    public int? Port { get; set; }

    public SensorProtocol Protocol { get; set; } = SensorProtocol.Simulation;

    public bool IsSimulation { get; set; } = true;

    public bool IsConnectionEnabled { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public Guid? GatewayId { get; set; }

    public bool HasActiveAlert { get; set; }

    public string? ActiveAlertMessage { get; set; }

    public AlertSeverity? ActiveAlertSeverity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}