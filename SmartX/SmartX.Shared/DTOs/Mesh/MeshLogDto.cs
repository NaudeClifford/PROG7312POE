using SmartX.Domain.Enums.Mesh;

namespace SmartX.Shared.DTOs.Mesh;

public class MeshLogDto
{
    public Guid Id { get; set; }
    public Guid GatewayId { get; set; }

    public Guid? NodeId { get; set; }
    public Guid? ConnectionId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }

    public MeshLogSeverity Severity { get; set; } = MeshLogSeverity.Information;

    public DateTime Timestamp { get; set; }
}