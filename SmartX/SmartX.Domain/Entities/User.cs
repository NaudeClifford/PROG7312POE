using SmartX.Domain.Enums;

namespace SmartX.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FirebaseUid { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Viewer;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}