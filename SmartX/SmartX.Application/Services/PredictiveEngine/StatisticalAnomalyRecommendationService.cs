
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;

namespace SmartX.Application.Services.PredictiveEngine;

public sealed class StatisticalAnomalyRecommendationService
    : IAnomalyRecommendationService
{
    private readonly ITelemetryRepository _telemetryRepository;

    public StatisticalAnomalyRecommendationService(
        ITelemetryRepository telemetryRepository)
    {
        _telemetryRepository = telemetryRepository;
    }

    public async Task<AnomalyRecommendationDto?> AnalyzeAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
            throw new ArgumentException("Sensor ID is required.");

        var telemetry =
            await _telemetryRepository.GetBySensorIdAsync(
                sensorId,
                cancellationToken);

        var readings = telemetry
            .Where(x => x.SensorId == sensorId)
            .OrderBy(x => x.Timestamp)
            .ToArray();
        System.Diagnostics.Debug.WriteLine(
    $"ANALYSIS SENSOR: {sensorId}");

        System.Diagnostics.Debug.WriteLine(
            $"READINGS FOUND: {readings.Length}");
        // Require enough history for a meaningful comparison.
        if (readings.Length < 6)
            return null;

        var latest = readings[^1];

        // Exclude the latest reading from the baseline.
        var baseline = readings
            .Take(readings.Length - 1)
            .TakeLast(20)
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
            return null;

        var severity = anomaly.SeverityScore >= 4
            ? "High"
            : "Medium";

        var explanation =
            $"{anomaly.Metric} is {anomaly.Observed:F2} " +
            $"{anomaly.Unit}; the recent baseline is " +
            $"{anomaly.Expected:F2} {anomaly.Unit}.";

        var action = anomaly.Metric switch
        {
            "Temperature" =>
                "Inspect cooling, ventilation, and sensor placement.",

            "Power" =>
                "Inspect the connected load and compare consumption.",

            "Voltage" =>
                "Verify supply voltage and electrical connections.",

            "Current" =>
                "Inspect the connected load for unexpected current draw.",

            _ =>
                "Inspect the sensor and verify the measurement."
        };

        // Recommendations are advisory; do not automatically
        // generate an executable shutdown command.
        return new AnomalyRecommendationDto
        {
            SensorId = sensorId,
            Metric = anomaly.Metric,
            Observed = anomaly.Observed,
            Expected = anomaly.Expected,
            Unit = anomaly.Unit,
            Severity = severity,
            Explanation = explanation,
            RecommendedAction = action,
            SuggestedCommand = null,
            Confidence = Math.Min(
                0.99,
                0.60 + anomaly.SeverityScore * 0.08),
            DetectedAt = DateTime.UtcNow
        };
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
            return null;

        var values = baselineValues
            .Where(x =>
                x.HasValue &&
                double.IsFinite(x.Value))
            .Select(x => x!.Value)
            .ToArray();

        if (values.Length < 5)
            return null;

        var mean = values.Average();

        var variance = values
            .Select(x => Math.Pow(x - mean, 2))
            .Average();

        var standardDeviation = Math.Sqrt(variance);

        var deviation =
            Math.Abs(observedValue.Value - mean);

        if (deviation < minimumDeviation ||
            deviation < Math.Max(
                standardDeviation * 3,
                minimumDeviation))
            return null;

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
