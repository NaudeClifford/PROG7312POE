using SmartX.Domain.Enums.Mesh;

namespace SmartX.Domain.Entities.Mesh;

public class MeshLog
{
    public Guid Id { get; set; }
    public Guid GatewayId { get; set; }

    public Guid? NodeId { get; set; }

    public Guid? ConnectionId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public MeshLogSeverity Severity { get; set; }
        = MeshLogSeverity.Information;

    public string? Details { get; set; }

    public DateTime Timestamp { get; set; }
        = DateTime.UtcNow;
}