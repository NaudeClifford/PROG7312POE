using SmartX.Domain.Enums;

namespace SmartX.Application.Commands.Company;

public class UpdateCompanyCommand
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}