namespace SmartX.Application.Requests.Company;

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
