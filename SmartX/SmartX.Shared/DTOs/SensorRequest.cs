namespace SmartX.Shared.DTOs;

public class SensorRequest
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;

    public int Category { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public Guid? GatewayId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}