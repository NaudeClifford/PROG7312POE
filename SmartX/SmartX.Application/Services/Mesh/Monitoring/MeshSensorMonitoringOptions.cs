namespace SmartX.Application.Services.Mesh.Monitoring;

public class MeshSensorMonitoringOptions
{
    public int HeartbeatTimeoutSeconds { get; set; } = 60;

    public int MonitoringIntervalSeconds { get; set; } = 15;
}