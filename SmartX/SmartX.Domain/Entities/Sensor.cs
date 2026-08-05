namespace SmartX.Domain.Entities;

public class Sensor
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Guid? GatewayId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}