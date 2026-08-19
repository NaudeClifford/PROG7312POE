

namespace SmartX.Shared.DTOs
{
    public class GatewayDto
    {
        public Guid Id { get; set; }

        public Guid CompanyId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? SerialNumber { get; set; }

        public string? IpAddress { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
