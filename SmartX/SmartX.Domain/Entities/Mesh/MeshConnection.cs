namespace SmartX.Domain.Entities.Mesh;

public class MeshConnection
{
    public Guid Id { get; set; }

    public Guid GatewayId { get; set; }

    public Guid SourceNodeId { get; set; }

    public Guid DestinationNodeId { get; set; }

    // Signal strength in dBm
    public double? SignalStrength { get; set; }

    // Link quality percentage
    public double LinkQuality { get; set; }

    // Communication latency in milliseconds
    public double Latency { get; set; }

    // Estimated transmission energy cost
    public double EnergyCost { get; set; }

    // Combined routing cost
    public double RoutingCost { get; set; }

    public bool IsActive { get; set; }

    public DateTime LastUpdated { get; set; }
        = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;
}