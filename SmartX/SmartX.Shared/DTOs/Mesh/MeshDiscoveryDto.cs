namespace SmartX.Shared.DTOs.Mesh;

public class MeshDiscoveryDto
{
    public Guid GatewayId { get; set; }

    public List<MeshNodeDto> DiscoveredNodes { get; set; } = [];

    public List<MeshLinkDto> DiscoveredLinks { get; set; } = [];

    public DateTime DiscoveredAt { get; set; }
}