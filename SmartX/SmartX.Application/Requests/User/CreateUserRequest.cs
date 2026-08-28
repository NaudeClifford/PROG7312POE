using SmartX.Domain.Enums;

namespace SmartX.Application.Requests.User;

public class CreateUserRequest
{
    public Guid CompanyId { get; set; }

    public string FirebaseUid { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;
}
