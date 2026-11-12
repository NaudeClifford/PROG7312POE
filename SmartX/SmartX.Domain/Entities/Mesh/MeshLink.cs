using SmartX.Domain.Enums.Mesh;

namespace SmartX.Domain.Entities.Mesh;

public class MeshLink
{
    public Guid Id { get; set; }

    public Guid GatewayId { get; set; }

    // Source node
    public Guid SourceNodeId { get; set; }

    public MeshNodeType SourceNodeType { get; set; }

    // Destination node
    public Guid TargetNodeId { get; set; }

    public MeshNodeType TargetNodeType { get; set; }

    // Connection measurements
    public double? SignalStrengthDbm { get; set; }

    public double? LatencyMilliseconds { get; set; }

    public double? EnergyCost { get; set; }

    // Connection status
    public bool IsAvailable { get; set; } = true;

    public DateTime LastSeenAt { get; set; } =
        DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } =
        DateTime.UtcNow;
}
