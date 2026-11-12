
namespace SmartX.Application.Services.Mesh.Monitoring;

public class MeshMonitoringOptions
{
    public int LinkTimeoutMinutes { get; set; } = 2;

    public int NodeTimeoutMinutes { get; set; } = 5;

    public double MinimumLinkQuality { get; set; } = 20;

    public double DegradedLinkQuality { get; set; } = 50;

    public double MaximumHealthyLatency { get; set; } = 100;

    public double MinimumNetworkAvailability { get; set; } = 95;

    public double MaximumLatencyMilliseconds { get; set; } = 100;

    public double MaximumEnergyCost { get; set; } = 100;

    public bool RecordFailedRoutes { get; set; } = true;
}
