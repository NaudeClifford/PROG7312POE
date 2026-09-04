namespace SmartX.Domain.Entities;

public class CompanyConfiguration
{
    public Guid CompanyId { get; set; }

    // API
    public bool UseCustomApi { get; set; }

    public string? ApiBaseUrl { get; set; }

    // Firebase
    public bool UseCustomFirebase { get; set; }

    public string? FirebaseProjectId { get; set; }

    public string? FirebaseApiKey { get; set; }

    public string? FirebaseServiceAccountPath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; }

}
