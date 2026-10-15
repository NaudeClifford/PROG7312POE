
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.WPF.Services.PredictiveEngine;

public sealed class PredictiveEngine : IPredictiveEngine
{
    private const int HistoryWindowSize = 5;
    private const int RecurringReadingThreshold = 3;
    private const int RecurringPatternBonus = 15;
    private const int MaximumSuggestions = 5;

    // Initial thresholds. Adjust for your equipment specifications.
    private const double TemperatureWarning = 32;
    private const double TemperatureCritical = 35;

    private const double VoltageWarning = 235;
    private const double VoltageCritical = 240;

    private const double CurrentWarning = 4.5;
    private const double CurrentCritical = 5;

    public IReadOnlyList<PredictiveSuggestion> GenerateWarnings(
        IEnumerable<TelemetryDto> telemetry)
    {
        var priorityQueue =
            new PriorityQueue<PredictiveSuggestion, int>();

        var normalQueue =
            new PriorityQueue<PredictiveSuggestion, int>();

        var telemetryBySensor = telemetry
            .GroupBy(reading => reading.SensorId);

        foreach (var sensorReadings in telemetryBySensor)
        {
            var history = sensorReadings
                .OrderByDescending(reading => reading.Timestamp)
                .Take(HistoryWindowSize)
                .ToList();

            if (history.Count == 0)
                continue;

            var latest = history[0];

            // Current threshold risk.
            var currentScore = CalculateTelemetryScore(latest);

            // Recurring anomalies in the recent history.
            var recurringReadings = history.Count(
                reading => CalculateTelemetryScore(reading) > 0);

            var isRecurring =
                recurringReadings >= RecurringReadingThreshold;

            // Rising trends based on timestamp-aware regression.
            var trend = CalculateTrend(history);

            // Do not use historical maximum severity as if it
            // were necessarily the sensor's current condition.
            if (currentScore == 0 &&
                !isRecurring &&
                trend.Score == 0)
            {
                continue;
            }

            var score = Math.Min(
                100,
                currentScore
                + (isRecurring ? RecurringPatternBonus : 0)
                + trend.Score);

            // Preserve a meaningful score for historical recurrence.
            if (currentScore == 0 && isRecurring)
                score = Math.Max(score, 30);

            var severity = DetermineSeverity(score);

            var title = currentScore > 0
                ? "Active telemetry warning"
                : isRecurring
                    ? "Recurring anomaly detected"
                    : "Rising telemetry trend";

            var warning = new PredictiveSuggestion
            {
                SensorId = latest.SensorId,
                Title = title,
                Description = BuildDescription(
                    latest,
                    history.Count,
                    recurringReadings,
                    isRecurring,
                    trend),
                Type = SuggestionType.DeviceWarning,
                Severity = severity,
                Score = score,
                RecommendedAction = BuildRecommendedAction(
                    latest,
                    isRecurring,
                    trend)
            };

            // Lower queue priorities are dequeued first.
            // Negative scores make higher-risk warnings come first.
            if (severity is AlertSeverity.Critical
                or AlertSeverity.High)
            {
                priorityQueue.Enqueue(warning, -score);
            }
            else
            {
                normalQueue.Enqueue(warning, -score);
            }
        }

        var result = new List<PredictiveSuggestion>();

        // Always process urgent warnings before routine suggestions.
        while (priorityQueue.Count > 0 &&
               result.Count < MaximumSuggestions)
        {
            result.Add(priorityQueue.Dequeue());
        }

        while (normalQueue.Count > 0 &&
               result.Count < MaximumSuggestions)
        {
            result.Add(normalQueue.Dequeue());
        }

        return result;
    }

    private static int CalculateTelemetryScore(
        TelemetryDto reading)
    {
        var score = 0;

        if (reading.Temperature >= TemperatureCritical)
            score += 30;
        else if (reading.Temperature >= TemperatureWarning)
            score += 15;

        if (reading.Voltage >= VoltageCritical)
            score += 30;
        else if (reading.Voltage >= VoltageWarning)
            score += 15;

        if (reading.Current >= CurrentCritical)
            score += 30;
        else if (reading.Current >= CurrentWarning)
            score += 15;

        return score;
    }

    private sealed record TrendResult(
        int Score,
        List<string> Details);

    private static TrendResult CalculateTrend(
        IReadOnlyList<TelemetryDto> history)
    {
        var details = new List<string>();
        var score = 0;

        score += CalculateMetricTrend(
            history,
            reading => reading.Temperature,
            TemperatureWarning,
            0.5,
            "Temperature",
            "°C",
            details);

        score += CalculateMetricTrend(
            history,
            reading => reading.Voltage,
            VoltageWarning,
            1.0,
            "Voltage",
            "V",
            details);

        score += CalculateMetricTrend(
            history,
            reading => reading.Current,
            CurrentWarning,
            0.2,
            "Current",
            "A",
            details);

        return new TrendResult(
            Math.Min(score, 30),
            details);
    }

    private static int CalculateMetricTrend(
        IReadOnlyList<TelemetryDto> history,
        Func<TelemetryDto, double?> selector,
        double warningThreshold,
        double minimumRisePerHour,
        string metricName,
        string unit,
        List<string> details)
    {
        var samples = history
            .Where(reading => selector(reading).HasValue)
            .Select(reading => new
            {
                reading.Timestamp,
                Value = selector(reading)!.Value
            })
            .GroupBy(reading => reading.Timestamp)
            .Select(group => group.First())
            .OrderBy(reading => reading.Timestamp)
            .ToList();

        if (samples.Count < 3)
            return 0;

        var firstTimestamp = samples[0].Timestamp;

        var points = samples
            .Select(sample => new
            {
                Hours = (
                    sample.Timestamp - firstTimestamp).TotalHours,
                sample.Value
            })
            .ToList();

        var elapsedHours = points[^1].Hours;

        if (elapsedHours <= 0)
            return 0;

        var meanX = points.Average(point => point.Hours);
        var meanY = points.Average(point => point.Value);

        var denominator = points.Sum(
            point => Math.Pow(point.Hours - meanX, 2));

        if (denominator <= 0)
            return 0;

        // Linear regression slope, expressed in units per hour.
        var slope = points.Sum(
            point =>
                (point.Hours - meanX) *
                (point.Value - meanY)) / denominator;

        var increasingPairs = 0;

        for (var i = 1; i < points.Count; i++)
        {
            if (points[i].Value > points[i - 1].Value)
                increasingPairs++;
        }

        var latestValue = points[^1].Value;

        // Require the reading to be below but near its threshold.
        var isApproachingThreshold =
            latestValue < warningThreshold &&
            latestValue >= warningThreshold * 0.8;

        var isTrendingUp =
            slope >= minimumRisePerHour &&
            increasingPairs >= points.Count - 2;

        if (!isTrendingUp || !isApproachingThreshold)
            return 0;

        details.Add(
            $"{metricName} is rising at approximately " +
            $"{slope:F2} {unit}/hour and approaching " +
            $"the warning threshold of " +
            $"{warningThreshold:F1} {unit}");

        return 15;
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

    private static string BuildDescription(
        TelemetryDto reading,
        int samplesChecked,
        int recurringReadings,
        bool isRecurring,
        TrendResult trend)
    {
        var problems = new List<string>();

        if (reading.Temperature >= TemperatureCritical)
        {
            problems.Add(
                $"High temperature: {reading.Temperature:F1} °C");
        }
        else if (reading.Temperature >= TemperatureWarning)
        {
            problems.Add(
                $"Elevated temperature: {reading.Temperature:F1} °C");
        }

        if (reading.Voltage >= VoltageCritical)
        {
            problems.Add(
                $"High voltage: {reading.Voltage:F1} V");
        }
        else if (reading.Voltage >= VoltageWarning)
        {
            problems.Add(
                $"Elevated voltage: {reading.Voltage:F1} V");
        }

        if (reading.Current >= CurrentCritical)
        {
            problems.Add(
                $"High current: {reading.Current:F1} A");
        }
        else if (reading.Current >= CurrentWarning)
        {
            problems.Add(
                $"Elevated current: {reading.Current:F1} A");
        }

        if (isRecurring)
        {
            problems.Add(
                $"Threshold anomalies occurred in " +
                $"{recurringReadings} of the latest " +
                $"{samplesChecked} samples");
        }

        problems.AddRange(trend.Details);

        if (problems.Count == 0)
        {
            problems.Add(
                "A recurring anomaly was detected in recent history, " +
                "but the latest reading is within configured thresholds");
        }

        return string.Join(". ", problems) + ".";
    }

    private static string BuildRecommendedAction(
        TelemetryDto reading,
        bool isRecurring,
        TrendResult trend)
    {
        var actions = new List<string>();

        if (reading.Temperature >= TemperatureWarning)
        {
            actions.Add(
                "Inspect ventilation, cooling, and operating conditions.");
        }

        if (reading.Voltage >= VoltageWarning)
        {
            actions.Add(
                "Verify the supply voltage against the equipment's " +
                "rated operating range.");
        }

        if (reading.Current >= CurrentWarning)
        {
            actions.Add(
                "Check equipment load and compare current draw " +
                "with rated limits.");
        }

        if (trend.Score > 0)
        {
            actions.Add(
                "Investigate the rising measurement before it " +
                "reaches the warning threshold.");
        }

        if (isRecurring)
        {
            actions.Add(
                "Review recent sensor history and check calibration.");
        }

        if (actions.Count == 0)
        {
            actions.Add(
                "Review recent telemetry and investigate " +
                "the recurring anomaly.");
        }

        return string.Join(" ", actions);
    }
}