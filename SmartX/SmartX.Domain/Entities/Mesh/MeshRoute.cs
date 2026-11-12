using SmartX.Domain.Enums.Mesh;

namespace SmartX.Domain.Entities.Mesh;

public class MeshRoute
{
    public Guid Id { get; set; }
    public Guid GatewayId { get; set; }

    public Guid SourceNodeId { get; set; }

    public Guid DestinationNodeId { get; set; }

    public List<Guid> NodePath { get; set; } = [];

    public RoutingAlgorithm Algorithm { get; set; }

    public double TotalCost { get; set; }

    public double TotalLatency { get; set; }

    public int HopCount { get; set; }

    public bool IsSuccessful { get; set; }

    public DateTime CalculatedAt { get; set; }
        = DateTime.UtcNow;
}