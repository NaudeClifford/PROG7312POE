using SmartX.Shared.DTOs;

namespace SmartX.Application.Authentication;

public class RegistrationResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public Guid CompanyId { get; set; }

    public Guid UserId { get; set; }

    public UserDto? User { get; set; }
}
