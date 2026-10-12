using SmartX.Domain.Enums;

namespace SmartX.Domain.Entities
{
    public class PredictiveSuggestion
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public Guid SensorId { get; init; }

        public string SensorName { get; set; } = "Unknown Sensor";

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public SuggestionType Type { get; init; }

        public AlertSeverity Severity { get; init; }

        public int Score { get; init; }

        public string RecommendedAction { get; init; } = string.Empty;

        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    }
}
