
using SmartX.Domain.Entities;
using SmartX.WPF.Models.History;

namespace SmartX.WPF.Services.PredictiveEngine;

public sealed class StatisticalAnomalyRecommendationService : IAnomalyRecommendationService
{
    public Task<AnomalyRecommendation?> AnalyzeAsync(
        Guid sensorId,
        IReadOnlyCollection<Telemetry> telemetry,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var readings = telemetry
            .Where(x => x.SensorId == sensorId)
            .OrderBy(x => x.Timestamp)
            .ToArray();

        // Require enough data to establish a baseline.
        if (readings.Length < 6)
            return Task.FromResult<AnomalyRecommendation?>(null);

        var latest = readings[^1];

        // Compare the latest reading against the preceding 20 readings.
        var baseline = readings
            .Skip(Math.Max(0, readings.Length - 21))
            .Take(readings.Length - Math.Max(0, readings.Length - 21) - 1)
            .ToArray();

        var candidates = new[]
        {
            CreateCandidate(
                "Temperature",
                latest.Temperature,
                baseline.Select(x => x.Temperature),
                "°C",
                5.0),

            CreateCandidate(
                "Power",
                latest.Power,
                baseline.Select(x => x.Power),
                "W",
                20.0),

            CreateCandidate(
                "Voltage",
                latest.Voltage,
                baseline.Select(x => x.Voltage),
                "V",
                10.0),

            CreateCandidate(
                "Current",
                latest.Current,
                baseline.Select(x => x.Current),
                "A",
                2.0)
        };

        var anomaly = candidates
            .Where(x => x is not null)
            .OrderByDescending(x => x!.SeverityScore)
            .FirstOrDefault();

        if (anomaly is null)
            return Task.FromResult<AnomalyRecommendation?>(null);

        var severity = anomaly.SeverityScore >= 4
            ? "High"
            : "Medium";

        var explanation =
            $"{anomaly.Metric} is {anomaly.Observed:F2} {anomaly.Unit}; " +
            $"the recent baseline is {anomaly.Expected:F2} {anomaly.Unit}.";

        var action = anomaly.Metric switch
        {
            "Temperature" =>
                "Inspect cooling, ventilation, and sensor placement. " +
                "Reduce load if the elevated temperature is confirmed.",

            "Power" =>
                "Inspect the connected load and compare consumption " +
                "with the expected operating state.",

            "Voltage" =>
                "Verify the supply voltage and inspect the power source " +
                "and electrical connections.",

            "Current" =>
                "Inspect the connected load for unexpected current draw.",

            _ => "Inspect the sensor and verify the measurement."
        };

        // No command is automatically inferred from an anomaly.
        // Configure an approved command mapping for the specific device
        // and operating condition before allowing command execution.
        var recommendation = new AnomalyRecommendation(
            sensorId,
            anomaly.Metric,
            anomaly.Observed,
            anomaly.Expected,
            anomaly.Unit,
            severity,
            explanation,
            action,
            SuggestedCommand: null,
            Confidence: Math.Min(
                0.99,
                0.60 + anomaly.SeverityScore * 0.08),
            DetectedAt: DateTime.UtcNow);

        return Task.FromResult<AnomalyRecommendation?>(
            recommendation);
    }

    private static Candidate? CreateCandidate(
        string metric,
        double? observedValue,
        IEnumerable<double?> baselineValues,
        string unit,
        double minimumDeviation)
    {
        if (!observedValue.HasValue ||
            !double.IsFinite(observedValue.Value))
        {
            return null;
        }

        var values = baselineValues
            .Where(x => x.HasValue && double.IsFinite(x.Value))
            .Select(x => x!.Value)
            .ToArray();

        if (values.Length < 5)
            return null;

        var mean = values.Average();

        var variance = values
            .Select(x => Math.Pow(x - mean, 2))
            .Average();

        var standardDeviation = Math.Sqrt(variance);
        var deviation = Math.Abs(observedValue.Value - mean);

        // Require both a meaningful absolute difference and a
        // statistically unusual difference from the baseline.
        if (deviation < minimumDeviation ||
            deviation < Math.Max(standardDeviation * 3, minimumDeviation))
        {
            return null;
        }

        var score = standardDeviation > 0
            ? deviation / standardDeviation
            : 4.0;

        return new Candidate(
            metric,
            observedValue.Value,
            mean,
            unit,
            score);
    }

    private sealed record Candidate(
        string Metric,
        double Observed,
        double Expected,
        string Unit,
        double SeverityScore);
}
