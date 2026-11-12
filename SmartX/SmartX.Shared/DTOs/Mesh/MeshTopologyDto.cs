namespace SmartX.Shared.DTOs.Mesh;

public class MeshTopologyDto
{
    public Guid GatewayId { get; set; }

    public IReadOnlyList<MeshNodeDto> Nodes { get; set; } = [];

    public IReadOnlyList<MeshLinkDto> Links { get; set; } = [];

    public int TotalNodes => Nodes.Count;

    public int TotalLinks => Links.Count;

    public int OnlineNodes =>
        Nodes.Count(x => x.Status == "Online");

    public int OfflineNodes =>
        Nodes.Count(x => x.Status == "Offline");
}