
namespace SmartX.WPF.Models.History;

public sealed record AnomalyRecommendation(
    Guid SensorId,
    string Metric,
    double ObservedValue,
    double ExpectedValue,
    string Unit,
    string Severity,
    string Explanation,
    string RecommendedAction,
    string? SuggestedCommand,
    double Confidence,
    DateTime DetectedAt);
