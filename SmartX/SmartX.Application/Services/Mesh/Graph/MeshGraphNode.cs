namespace SmartX.Application.Services.Mesh.Graph;

public class MeshGraphNode
{
    public Guid Id { get; set; }

    public string DeviceIdentifier { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsGateway { get; set; }

    public bool IsOnline { get; set; }

    public List<MeshGraphEdge> Connections { get; set; } = [];
}