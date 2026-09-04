using SmartX.Domain.Enums;

namespace SmartX.Application.Requests.User;

public class UpdateUserRequest
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string FirebaseUid { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; }
}
