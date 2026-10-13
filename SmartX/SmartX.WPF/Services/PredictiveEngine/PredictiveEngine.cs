using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.WPF.Services.PredictiveEngine;

public sealed class PredictiveEngine : IPredictiveEngine
{
    public IReadOnlyList<PredictiveSuggestion> GenerateWarnings(
        IEnumerable<TelemetryDto> telemetry)
    {
        var warnings = new PriorityQueue<PredictiveSuggestion, int>();

        var telemetryBySensor = telemetry.GroupBy(reading => reading.SensorId);

        foreach (var sensorReadings in telemetryBySensor)
        {
            var latest = sensorReadings
                .MaxBy(reading => reading.Timestamp);

            if (latest is null)
                continue;

            var score = CalculateTelemetryScore(latest);

            if (score == 0)
                continue;

            var warning = new PredictiveSuggestion
            {
                SensorId = latest.SensorId,
                Title = "Potential device problem",
                Description = BuildDescription(latest),
                Type = SuggestionType.DeviceWarning,
                Severity = DetermineSeverity(score),
                Score = score
            };

            // Negative priority means higher scores come out first.
            warnings.Enqueue(warning, -score);
        }

        var result = new List<PredictiveSuggestion>();

        while (warnings.Count > 0 && result.Count < 5)
        {
            result.Add(warnings.Dequeue());
        }

        return result;
    }

    private static int CalculateTelemetryScore(
        TelemetryDto reading)
    {
        var score = 0;

        if (reading.Temperature >= 35)
            score += 30;
        else if (reading.Temperature >= 32)
            score += 15;

        if (reading.Voltage >= 240)
            score += 30;
        else if (reading.Voltage >= 235)
            score += 15;

        if (reading.Current >= 5)
            score += 30;
        else if (reading.Current >= 4.5)
            score += 15;

        return score;
    }

    private static AlertSeverity DetermineSeverity(int score)
    {
        return score switch
        {
            >= 90 => AlertSeverity.Critical,
            >= 60 => AlertSeverity.High,
            >= 30 => AlertSeverity.Medium,
            _ => AlertSeverity.Low
        };
    }

    private static string BuildDescription(TelemetryDto reading)
    {
        var problems = new List<string>();

        if (reading.Temperature >= 35)
            problems.Add(
                $"High temperature: {reading.Temperature:F1}°C");
        else if (reading.Temperature >= 32)
            problems.Add(
                $"Elevated temperature: {reading.Temperature:F1}°C");

        if (reading.Voltage >= 240)
            problems.Add($"High voltage: {reading.Voltage:F1}V");
        else if (reading.Voltage >= 235)
            problems.Add($"Elevated voltage: {reading.Voltage:F1}V");

        if (reading.Current >= 5)
            problems.Add($"High current: {reading.Current:F1}A");
        else if (reading.Current >= 4.5)
            problems.Add($"Elevated current: {reading.Current:F1}A");

        return string.Join(". ", problems);
    }
}