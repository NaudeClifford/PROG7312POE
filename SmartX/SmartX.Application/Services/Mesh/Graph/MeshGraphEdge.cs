namespace SmartX.Application.Services.Mesh.Graph;

public class MeshGraphEdge
{
    public Guid DestinationNodeId { get; set; }

    public double RoutingCost { get; set; }

    public double Latency { get; set; }

    public double EnergyCost { get; set; }

    public double LinkQuality { get; set; }

    public bool IsActive { get; set; }
}