namespace SmartX.Shared.DTOs.Mesh;

public class MeshLinkDto
{
    public Guid Id { get; set; }
    public Guid GatewayId { get; set; }

    public Guid SourceNodeId { get; set; }
    public Guid TargetNodeId { get; set; }

    public double? SignalStrengthDbm { get; set; }
    public double? LatencyMilliseconds { get; set; }
    public double? EnergyCost { get; set; }

    public bool IsAvailable { get; set; }

    public DateTime LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public double LinkQuality { get; set; }
}