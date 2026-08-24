namespace SmartX.Domain.Entities;

public class Alarm
{
    public Guid Id { get; set; }

    public Guid SensorId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public double? TriggerValue { get; set; }

    public DateTime TriggeredAt { get; set; }

    public bool IsAcknowledged { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}