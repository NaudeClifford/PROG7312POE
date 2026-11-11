using SmartX.Domain.Enums;

namespace SmartX.Shared.DTOs.Sensors;

public class SensorDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;

    public int Category { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public int? Port { get; set; }

    public SensorProtocol Protocol { get; set; }

    public bool IsSimulation { get; set; }

    public bool IsConnectionEnabled { get; set; }

    public bool IsActive { get; set; }

    public bool HasActiveAlert { get; set; }

    public string? ActiveAlertMessage { get; set; }

    public AlertSeverity? ActiveAlertSeverity { get; set; }

    public Guid? GatewayId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

}