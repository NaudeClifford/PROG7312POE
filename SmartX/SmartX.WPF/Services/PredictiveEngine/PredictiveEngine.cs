using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Domain.shared;
using SmartX.Shared.DTOs;
using SmartX.Shared.DTOs.Telemetry;

namespace SmartX.WPF.Services.PredictiveEngine;

public sealed class PredictiveEngine : IPredictiveEngine
{
    private const int HistoryWindowSize = 20;
    private const int ReadingsBeforeCommand = 10;
    private const int ReadingsAfterCommand = 10;
    private const int MinimumReadingsForComparison = 5;


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
        IEnumerable<TelemetryDto> telemetry,
        IEnumerable<UserBehaviorDto> userBehaviors)
    {
        var priorityQueue = new PriorityQueue<PredictiveSuggestion, int>();

        var normalQueue = new PriorityQueue<PredictiveSuggestion, int>();

        var allTelemetry = telemetry.ToList();

        var behaviorBySensor = userBehaviors
            .GroupBy(behavior => behavior.SensorId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(
                    behavior => behavior.Timestamp).ToList());

        var telemetryBySensor = allTelemetry.GroupBy(reading => reading.SensorId);

        foreach (var sensorReadings in telemetryBySensor)
        {
            var allReadings = sensorReadings
                .OrderBy(reading => reading.Timestamp)
                .ToList();

            var history = allReadings
                .OrderByDescending(reading => reading.Timestamp)
                .Take(HistoryWindowSize)
                .ToList();

            if (history.Count == 0) continue;

            var latest = history[0];

            behaviorBySensor.TryGetValue(latest.SensorId, out var sensorBehaviors);

            sensorBehaviors ??= new List<UserBehaviorDto>();

            // Current threshold risk.
            var currentScore = CalculateTelemetryScore(latest);

            // Recurring anomalies in the recent history.
            var recurringReadings = history.Count(reading => CalculateTelemetryScore(reading) > 0);

            var isRecurring = recurringReadings >= RecurringReadingThreshold;

            // Rising trends based on timestamp-aware regression.
            var trend = CalculateTrend(history);

            // Do not use historical maximum severity as if it
            // were necessarily the sensor's current condition.
            if (currentScore == 0 && !isRecurring && trend.Score == 0) continue;

            var score = Math.Min(100, currentScore + (isRecurring ? RecurringPatternBonus : 0) + trend.Score);

            // Preserve a meaningful score for historical recurrence.
            if (currentScore == 0 && isRecurring) score = Math.Max(score, 30);

            var severity = DetermineSeverity(score);

            var title = currentScore > 0 ? "Active telemetry warning" : isRecurring
                    ? "Recurring anomaly detected" : "Rising telemetry trend";

            var (recommendedCommand, recommendationReason) = BuildRecommendedCommand(latest, sensorBehaviors);

            var warning = new PredictiveSuggestion
            {
                SensorId = latest.SensorId,
                Title = title,
                Description = BuildDescription(latest, history.Count, recurringReadings, isRecurring, trend, sensorBehaviors),
                Type = SuggestionType.DeviceWarning,
                Severity = severity,
                Score = score,
                RecommendedAction = BuildRecommendedAction(latest, isRecurring, trend, sensorBehaviors, allReadings),
                RecommendedCommand = recommendedCommand,
                RecommendationReason = recommendationReason
            };

            // Lower queue priorities are dequeued first.
            // Negative scores make higher-risk warnings come first.
            if (severity is AlertSeverity.Critical or AlertSeverity.High) priorityQueue.Enqueue(warning, -score);

            else normalQueue.Enqueue(warning, -score);

        }

        var result = new List<PredictiveSuggestion>();

        // Always process urgent warnings before routine suggestions.
        while (priorityQueue.Count > 0 &&
               result.Count < MaximumSuggestions) result.Add(priorityQueue.Dequeue());


        while (normalQueue.Count > 0 &&
               result.Count < MaximumSuggestions) result.Add(normalQueue.Dequeue());


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

    private sealed record TrendResult(int Score, List<string> Details);

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

        return new TrendResult(Math.Min(score, 30), details);
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

        if (samples.Count < 3) return 0;

        var firstTimestamp = samples[0].Timestamp;

        var points = samples
            .Select(sample => new
            {
                Hours = (sample.Timestamp - firstTimestamp).TotalHours,
                sample.Value
            }).ToList();

        var elapsedHours = points[^1].Hours;

        if (elapsedHours <= 0) return 0;

        var meanX = points.Average(point => point.Hours);
        var meanY = points.Average(point => point.Value);

        var denominator = points.Sum(
            point => Math.Pow(point.Hours - meanX, 2));

        if (denominator <= 0) return 0;

        // Linear regression slope, expressed in units per hour.
        var slope = points.Sum(point => (point.Hours - meanX) * (point.Value - meanY)) / denominator;

        var increasingPairs = 0;

        for (var i = 1; i < points.Count; i++) if (points[i].Value > points[i - 1].Value)
            increasingPairs++;

        var latestValue = points[^1].Value;

        // Require the reading to be below but near its threshold.
        var isApproachingThreshold =
            latestValue < warningThreshold &&
            latestValue >= warningThreshold * 0.8;

        var isTrendingUp =
            slope >= minimumRisePerHour &&
            increasingPairs >= points.Count - 2;

        if (!isTrendingUp || !isApproachingThreshold) return 0;

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
        TrendResult trend,
        IReadOnlyList<UserBehaviorDto> behaviors)
    {
        var problems = new List<string>();

        if (reading.Temperature >= TemperatureCritical) problems.Add(
                $"High temperature: {reading.Temperature:F1} °C");

        else if (reading.Temperature >= TemperatureWarning) problems.Add(
                $"Elevated temperature: {reading.Temperature:F1} °C");


        if (reading.Voltage >= VoltageCritical) problems.Add(
                $"High voltage: {reading.Voltage:F1} V");

        else if (reading.Voltage >= VoltageWarning) problems.Add(
                $"Elevated voltage: {reading.Voltage:F1} V");


        if (reading.Current >= CurrentCritical) problems.Add(
                $"High current: {reading.Current:F1} A");

        else if (reading.Current >= CurrentWarning) problems.Add(
                $"Elevated current: {reading.Current:F1} A");


        if (isRecurring) problems.Add(
                $"Threshold anomalies occurred in " +
                $"{recurringReadings} of the latest " +
                $"{samplesChecked} samples");


        if (behaviors.Count > 0)
        {
            var lastCommand = behaviors[0];

            var status = lastCommand.Succeeded
                ? "recorded as successful"
                : "recorded as unsuccessful";

            problems.Add(
                $"The latest recorded command was " +
                $"'{lastCommand.Command}', {status}");

            if (!string.IsNullOrWhiteSpace(lastCommand.Outcome))
                problems.Add($"Recorded outcome: {lastCommand.Outcome}");
        }

        problems.AddRange(trend.Details);

        if (problems.Count == 0) problems.Add(
                "A recurring anomaly was detected in recent history, " +
                "but the latest reading is within configured thresholds");

        return string.Join(". ", problems) + ".";
    }

    private static string BuildRecommendedAction(TelemetryDto reading, bool isRecurring, TrendResult trend,
        IReadOnlyList<UserBehaviorDto> behaviors, IReadOnlyList<TelemetryDto> telemetryHistory)
    {
        var actions = new List<string>();

        var warningTypes = new List<string>();

        if (reading.Temperature >= TemperatureWarning)
            warningTypes.Add("Temperature");

        if (reading.Voltage >= VoltageWarning)
            warningTypes.Add("Voltage");

        if (reading.Current >= CurrentWarning)
            warningTypes.Add("Current");

        // Find commands relevant to the current warning.
        var relevantCommands = behaviors
            .Where(behavior => warningTypes.Any(
                warningType => SensorCommandCatalog.IsCommandRelevant(behavior.Command, warningType)))
            .OrderByDescending(behavior => behavior.Timestamp)
            .ToList();

        var lastCommand = relevantCommands.FirstOrDefault();

        // Analyze temperature before and after the latest
        // relevant temperature command.
        var temperatureCommand = relevantCommands
            .FirstOrDefault(behavior => SensorCommandCatalog.IsCommandRelevant(
                    behavior.Command, "Temperature"));

        if (temperatureCommand != null)
        {
            var (beforeReadings, afterReadings) =
                GetBalancedReadingsAroundCommand(
                    telemetryHistory,
                    temperatureCommand.Timestamp);

            var beforeTemperatures = beforeReadings
                .Where(r => r.Temperature.HasValue)
                .Select(r => r.Temperature!.Value)
                .ToList();

            var afterTemperatures = afterReadings
                .Where(r => r.Temperature.HasValue)
                .Select(r => r.Temperature!.Value)
                .ToList();

            if (beforeTemperatures.Count >= MinimumReadingsForComparison &&
                afterTemperatures.Count >= MinimumReadingsForComparison)
            {
                var beforeAverage = beforeTemperatures.Average();
                var afterAverage = afterTemperatures.Average();

                var temperatureChange =
                    afterAverage - beforeAverage;

                if (temperatureChange < 0) actions.Add(
                        $"Average temperature decreased by " +
                        $"{Math.Abs(temperatureChange):F1} °C " +
                        "after the recorded command.");

                else if (temperatureChange > 0) actions.Add(
                        $"Average temperature increased by " +
                        $"{temperatureChange:F1} °C " +
                        "after the recorded command.");

                else actions.Add(
                        "Average temperature remained unchanged " +
                        "after the recorded command.");

            }
            else actions.Add(
                "Insufficient telemetry to evaluate the temperature command. " +
                $"Valid readings before: {beforeTemperatures.Count}; " +
                $"after: {afterTemperatures.Count}. " +
                $"At least {MinimumReadingsForComparison} valid readings " +
                "on each side are required.");
        }

        // Current telemetry recommendations.
        if (reading.Temperature >= TemperatureWarning) actions.Add(
                "Inspect ventilation, cooling, and operating conditions.");


        if (reading.Voltage >= VoltageWarning) actions.Add(
                "Verify the supply voltage against the equipment's " +
                "rated operating range.");


        if (reading.Current >= CurrentWarning) actions.Add(
                "Check equipment load and compare current draw " +
                "with rated limits.");


        if (trend.Score > 0) actions.Add(
                "Investigate the rising measurement before it " +
                "reaches the warning threshold.");


        if (isRecurring) actions.Add(
                "Review recent sensor history and check calibration.");


        // Report the latest relevant command once.
        if (lastCommand != null)
        {
            if (!lastCommand.Succeeded) actions.Add(
                    $"The relevant command '{lastCommand.Command}' " +
                    "was unsuccessful. Review its recorded outcome " +
                    "before considering another attempt.");

            else actions.Add(
                    $"The relevant command '{lastCommand.Command}' " +
                    "was recorded as successful. Verify whether the " +
                    "latest telemetry shows improvement.");


            if (!string.IsNullOrWhiteSpace(lastCommand.Outcome)) actions.Add(
                    $"Recorded command outcome: {lastCommand.Outcome}");

        }

        // Report older failed commands without duplicating the latest.
        foreach (var behavior in relevantCommands.Skip(1)
                     .Where(behavior => !behavior.Succeeded)) actions.Add(
                $"The previous command '{behavior.Command}' " +
                "was unsuccessful. Review its recorded outcome " +
                "before attempting another action.");


        if (actions.Count == 0) actions.Add(
                "Review recent telemetry and investigate " +
                "the recurring anomaly.");


        return string.Join(" ", actions);
    }



    private static (string? Command, string Reason) BuildRecommendedCommand(
        TelemetryDto reading,
        IEnumerable<UserBehaviorDto> behaviors)
    {
        // Identify the active telemetry warnings.
        var temperatureCritical =
            reading.Temperature >= TemperatureCritical;

        var voltageCritical =
            reading.Voltage >= VoltageCritical;

        var currentCritical =
            reading.Current >= CurrentCritical;

        var temperatureWarning =
            reading.Temperature >= TemperatureWarning;

        var voltageWarning =
            reading.Voltage >= VoltageWarning;

        var currentWarning =
            reading.Current >= CurrentWarning;

        var hasCriticalWarning =
            temperatureCritical ||
            voltageCritical ||
            currentCritical;

        var hasWarning =
            temperatureWarning ||
            voltageWarning ||
            currentWarning;

        // No corrective command is necessary when telemetry
        // is within the configured thresholds.
        if (!hasWarning)
        {
            return (
                null,
                "No active telemetry threshold requires a corrective command."
            );
        }

        // Keep previously successful, supported commands as
        // candidates, but never recommend arbitrary command text.
        var successfulCommands = behaviors
            .Where(b => b.Succeeded)
            .Where(b => !string.IsNullOrWhiteSpace(b.Command))
            .OrderByDescending(b => b.Timestamp)
            .Select(b => b.Command.Trim())
            .Where(SensorCommandCatalog.IsSupported).ToList();

        // Prefer a previously successful RESET when available.
        var previousReset = successfulCommands
            .FirstOrDefault(c =>
                c.Equals("RESET", StringComparison.OrdinalIgnoreCase));

        if (previousReset is not null)
        {
            return (
                previousReset.ToUpperInvariant(),
                "RESET is supported by the command service and has " +
                "previously been recorded as successful. Review the " +
                "equipment state and confirm that resetting is safe."
            );
        }

        // A critical warning needs additional caution.
        // RESET is a supported command, but its safety must be
        // confirmed for the particular device before execution.
        if (hasCriticalWarning)
        {
            return (
                "RESET",
                "Critical telemetry detected. RESET is an available " +
                "corrective command, but it has not been verified as safe " +
                "for this particular critical condition. Confirm the " +
                "equipment safety procedure before applying it."
            );
        }

        // For non-critical warnings, prefer a supported command
        // already recorded as successful, if one exists.
        var previousCommand = successfulCommands.FirstOrDefault();

        if (previousCommand is not null)
        {
            return (
                previousCommand.ToUpperInvariant(),
                $"The command '{previousCommand}' was previously recorded " +
                "as successful. Review its suitability for the current " +
                "telemetry warning before sending it."
            );
        }

        // Supported fallback for a non-critical warning.
        // RESET is used because it is explicitly supported by your service.
        return (
            "RESET",
            "A non-critical telemetry warning was detected. RESET is " +
            "supported by the command service and is suggested for review. " +
            "Confirm that resetting is appropriate for this device first."
        );
    }



    private static (List<TelemetryDto> Before, List<TelemetryDto> After)
    GetBalancedReadingsAroundCommand(IEnumerable<TelemetryDto> telemetry, DateTime commandTimestamp)
    {
        var before = telemetry
            .Where(reading => reading.Timestamp < commandTimestamp)
            .OrderByDescending(reading => reading.Timestamp)
            .Take(ReadingsBeforeCommand)
            .OrderBy(reading => reading.Timestamp)
            .ToList();

        var after = telemetry
            .Where(reading => reading.Timestamp > commandTimestamp)
            .OrderBy(reading => reading.Timestamp)
            .Take(ReadingsAfterCommand)
            .ToList();

        return (before, after);
    }


}