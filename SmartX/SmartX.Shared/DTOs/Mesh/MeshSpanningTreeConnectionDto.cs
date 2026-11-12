
namespace SmartX.Shared.DTOs.Mesh;

public class MeshSpanningTreeConnectionDto
{
    public Guid SourceNodeId { get; set; }

    public Guid DestinationNodeId { get; set; }

    public double Cost { get; set; }

    public double EnergyCost { get; set; }
}
