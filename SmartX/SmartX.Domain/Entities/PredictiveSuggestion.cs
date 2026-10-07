using SmartX.Domain.Enums;

namespace SmartX.Domain.Entities
{
    public class PredictiveSuggestion
    {
        public Guid? SensorId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public SuggestionType Type{ get; set; }

        public AlertSeverity Severity { get; set; }

        public int Score { get; set; }

        public string? SuggestedCommand { get; set; }
    }
}
