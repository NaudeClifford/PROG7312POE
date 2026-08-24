namespace SmartX.Domain.Entities;

public class AuditLogDto
{
    public Guid Id { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? GatewayId { get; set; }

    public Guid? SensorId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }

    public string? Details { get; set; }


}