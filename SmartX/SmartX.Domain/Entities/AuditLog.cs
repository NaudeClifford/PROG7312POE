
namespace SmartX.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }

        public Guid CompanyId { get; set; }

        public Guid? UserId { get; set; }

        public string EntityType { get; set; } = string.Empty;

        public Guid EntityId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
