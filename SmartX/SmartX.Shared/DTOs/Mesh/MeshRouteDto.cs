using SmartX.Domain.Enums.Mesh;

namespace SmartX.Shared.DTOs.Mesh;

public class MeshRouteDto
{
    public Guid Id { get; set; }

    public Guid GatewayId { get; set; }

    public Guid SourceNodeId { get; set; }

    public Guid DestinationNodeId { get; set; }

    public List<Guid> NodePath { get; set; } = new List<Guid>();

    public RoutingAlgorithm Algorithm { get; set; }

    public double TotalCost { get; set; }

    public double TotalLatency { get; set; }

    public int HopCount { get; set; }

    public bool IsSuccessful { get; set; }

    public DateTime CalculatedAt { get; set; }
}