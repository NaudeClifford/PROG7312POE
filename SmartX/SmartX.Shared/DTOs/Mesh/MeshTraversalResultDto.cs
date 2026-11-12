
namespace SmartX.Shared.DTOs.Mesh;

public class MeshTraversalResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid StartNodeId { get; set; }

    public List<Guid> VisitedNodeIds { get; set; } = [];

    public int TotalVisited { get; set; }
}
