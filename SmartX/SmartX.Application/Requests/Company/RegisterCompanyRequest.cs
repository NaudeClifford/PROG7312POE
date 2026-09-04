using SmartX.Domain.Enums;

namespace SmartX.Application.Requests.Company;

public class RegisterCompanyRequest
{
    public string CompanyName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;


    public string IdToken { get; set; } = string.Empty;


    public string DisplayName { get; set; } = string.Empty;
}
