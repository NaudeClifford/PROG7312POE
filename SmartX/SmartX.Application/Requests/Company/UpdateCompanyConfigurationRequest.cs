namespace SmartX.Application.Requests.Company;

public class UpdateCompanyConfigurationRequest
{
    public Guid CompanyId { get; set; }

    public bool UseCustomApi { get; set; }

    public string ApiBaseUrl { get; set; } = string.Empty;

    public bool UseCustomFirebase { get; set; }

    public string FirebaseProjectId { get; set; } = string.Empty;

    public string FirebaseApiKey { get; set; } = string.Empty;
}
