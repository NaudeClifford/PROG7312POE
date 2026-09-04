
namespace SmartX.Shared.DTOs
{
    public class CompanyDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public bool OnboardingComplete { get; set; }

        public bool DeletionRequested { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
