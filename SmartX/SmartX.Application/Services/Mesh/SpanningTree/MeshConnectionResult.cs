namespace SmartX.Application.Services.Mesh.SpanningTree;

public class MeshConnectionResult
{
    public Guid SourceNodeId { get; set; }

    public Guid DestinationNodeId { get; set; }

    public double Cost { get; set; }

    public double EnergyCost { get; set; }
}