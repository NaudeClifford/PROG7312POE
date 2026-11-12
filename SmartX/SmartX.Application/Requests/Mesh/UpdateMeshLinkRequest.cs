namespace SmartX.Shared.DTOs.Mesh.Requests;

public class UpdateMeshLinkRequest
{
    public Guid Id { get; set; }

    public Guid GatewayId { get; set; }

    public Guid SourceNodeId { get; set; }

    public Guid TargetNodeId { get; set; }

    public double? SignalStrengthDbm { get; set; }

    public double? LatencyMilliseconds { get; set; }

    public double? EnergyCost { get; set; }

    public bool IsAvailable { get; set; }
}