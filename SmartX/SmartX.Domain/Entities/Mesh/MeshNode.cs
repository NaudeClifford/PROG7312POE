using SmartX.Domain.Enums.Mesh;

namespace SmartX.Domain.Entities.Mesh;

public class MeshNode
{
    public Guid Id { get; set; }

    public Guid GatewayId { get; set; }
    public Guid DeviceId { get; set; }

    public MeshNodeType NodeType { get; set; }

    public string DeviceIdentifier { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public MeshNodeStatus Status { get; set; }
        = MeshNodeStatus.Offline;

    public bool IsGateway { get; set; }

    public double? BatteryLevel { get; set; }

    public DateTime? LastSeen { get; set; }

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; }
        = DateTime.UtcNow;
}