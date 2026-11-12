namespace SmartX.Shared.DTOs.Mesh;

public class MeshConnectionDto
{
    public Guid Id { get; set; }

    public Guid GatewayId { get; set; }

    public Guid SourceNodeId { get; set; }

    public Guid DestinationNodeId { get; set; }

    public double? SignalStrength { get; set; }

    public double LinkQuality { get; set; }

    public double Latency { get; set; }

    public double EnergyCost { get; set; }

    public double RoutingCost { get; set; }

    public bool IsActive { get; set; }

    public DateTime LastUpdated { get; set; }

    public DateTime CreatedAt { get; set; }
}