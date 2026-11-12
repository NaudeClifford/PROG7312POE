namespace SmartX.Shared.DTOs.Mesh;

public class MeshOperationResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid? GatewayId { get; set; }

    public Guid? LinkId { get; set; }

    public int Changes { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}