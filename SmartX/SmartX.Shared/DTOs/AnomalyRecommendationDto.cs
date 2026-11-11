
namespace SmartX.Shared.DTOs
{
    public sealed class AnomalyRecommendationDto
    {
        public Guid SensorId { get; set; }

        public string Metric { get; set; } = string.Empty;

        public double Observed { get; set; }

        public double Expected { get; set; }

        public string Unit { get; set; } = string.Empty;

        public string Severity { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public string RecommendedAction { get; set; } = string.Empty;

        public string? SuggestedCommand { get; set; }

        public double Confidence { get; set; }

        public DateTime DetectedAt { get; set; }
    }
}
