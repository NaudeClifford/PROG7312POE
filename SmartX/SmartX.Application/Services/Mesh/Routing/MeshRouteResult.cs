namespace SmartX.Application.Services.Mesh.Routing;

public class MeshRouteResult
{
    public bool Success { get; set; }

    public Guid SourceNodeId { get; set; }

    public Guid DestinationNodeId { get; set; }

    public List<Guid> NodePath { get; set; } = [];

    public double TotalCost { get; set; }

    public double TotalLatency { get; set; }

    public double TotalEnergyCost { get; set; }

    public int HopCount { get; set; }

    public string Message { get; set; } = string.Empty;
}