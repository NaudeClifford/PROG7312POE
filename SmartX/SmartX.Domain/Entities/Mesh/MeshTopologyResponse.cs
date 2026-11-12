
namespace SmartX.Domain.Entities.Mesh;

public class MeshTopologyResponse
{
    public Guid GatewayId { get; set; }

    public IReadOnlyList<MeshNode> Nodes { get; set; } = [];

    public IReadOnlyList<MeshLink> Links { get; set; } = [];
}