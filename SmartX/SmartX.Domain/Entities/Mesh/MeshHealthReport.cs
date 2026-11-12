using SmartX.Domain.Enums.Mesh;

namespace SmartX.Domain.Entities.Mesh;

public class MeshHealthReport
{
    public Guid GatewayId { get; set; }

    public MeshHealthStatus Status { get; set; }

    public int TotalNodes { get; set; }

    public int OnlineNodes { get; set; }

    public int OfflineNodes { get; set; }

    public int DegradedNodes { get; set; }

    public int UnreachableNodes { get; set; }

    public int TotalLinks { get; set; }

    public int AvailableLinks { get; set; }

    public int UnavailableLinks { get; set; }

    public int TotalRoutes { get; set; }

    public int SuccessfulRoutes { get; set; }

    public int FailedRoutes { get; set; }

    public double AverageLatency { get; set; }

    public double NetworkAvailability { get; set; }

    public DateTime GeneratedAt { get; set; }

    public List<string> Warnings { get; set; } = new();
}