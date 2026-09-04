using SmartX.Shared.DTOs;

namespace SmartX.Application.Services.Registration;

public class RegistrationResultDto
{
    public Guid CompanyId { get; set; }

    public UserDto? User { get; set; }
}
