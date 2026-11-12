
namespace SmartX.Shared.DTOs.Mesh;

public class MeshSpanningTreeDto
{
    public Guid GatewayId { get; set; }

    public Guid StartNodeId { get; set; }

    public List<MeshSpanningTreeConnectionDto> Connections { get; set; } = [];

    public int TotalConnections { get; set; }

    public double TotalCost { get; set; }

    public double TotalEnergyCost { get; set; }
}
