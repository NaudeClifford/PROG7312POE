namespace SmartX.Shared.DTOs.Mesh;

public class MeshNodeDto
{
    public Guid Id { get; set; }
    public Guid GatewayId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;

    public string NodeType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public bool IsGateway { get; set; }

    public DateTime? LastSeen { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}