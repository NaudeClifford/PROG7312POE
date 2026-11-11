using SmartX.Domain.Enums;

namespace SmartX.Shared.DTOs;

public sealed class SensorAlert
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string SensorName { get; init; } = string.Empty;

    public Guid SensorId { get; init; }

    public string Message { get; init; } = string.Empty;

    public AlertSeverity Severity { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}