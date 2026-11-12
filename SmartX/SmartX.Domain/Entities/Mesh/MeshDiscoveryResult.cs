namespace SmartX.Domain.Entities.Mesh;

public class MeshDiscoveryResult
{
    public List<MeshNode> DiscoveredNodes { get; set; } = [];

    public List<MeshLink> DiscoveredLinks { get; set; } = [];

    public int TotalNodes => DiscoveredNodes.Count;

    public int TotalLinks => DiscoveredLinks.Count;

    public DateTime DiscoveredAt { get; set; }
        = DateTime.UtcNow;
}