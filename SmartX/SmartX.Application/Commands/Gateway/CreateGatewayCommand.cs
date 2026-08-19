using SmartX.Domain.Enums;

namespace SmartX.Application.Commands.Sensors;

public class CreateGatewayCommand
{

    public Guid CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public string? IpAddress { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}