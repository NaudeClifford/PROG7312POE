namespace SmartX.Shared.DTOs.Mesh;

public class MeshRouteDto
{
    public Guid SourceNodeId { get; set; }

    public Guid DestinationNodeId { get; set; }

    public List<Guid> NodePath { get; set; } = [];

    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public double TotalCost { get; set; }

    public double TotalLatency { get; set; }

    public double TotalEnergyCost { get; set; }

    public int HopCount { get; set; }
}