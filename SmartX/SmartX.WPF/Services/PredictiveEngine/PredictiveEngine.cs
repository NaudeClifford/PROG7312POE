using SmartX.Application.Services.Alerts;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Shared.DTOs.Telemetry;
using System.Windows.Ink;

namespace SmartX.WPF.Services.PredictiveEngine
{
    public class PredictiveEngine : IPredictiveEngine
    {
        public IReadOnlyList<PredictiveSuggestion> GenerateSuggestions(
                IEnumerable<TelemetryDto> telemetryDto,
                IEnumerable<SensorAlert> alerts)
        {
            var suggestions = new PriorityQueue<PredictiveSuggestion, int>();

            var telemetryBySensor = telemetryDto.GroupBy(t => t.SensorId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var sensorTelemetry in telemetryBySensor)
            {
                var readings = sensorTelemetry.OrderByDescending(x => x.Timestamp).ToList();

                if (readings.Count == 0)
                {
                    continue;
                }

                var latest = readings[0];

                var score = CalculateTelemetryScore(readings);

                if (score <= 0)
                {
                    continue;
                }

                var suggestion = new PredictiveSuggestion
                {
                    SensorId = latest.SensorId,
                    Title = "Potential device problem",
                    Description = BuildDescription(readings, score),
                    Type = SuggestionType.Telemetry,
                    Severity = DetermineSeverity(score),
                    Score = score,
                };

                suggestions.Enqueue(suggestion, -score);
            }
            foreach (var alert in alerts)
            {
                var suggestion = CreateAlertSuggestion(alert);

                suggestions.Enqueue(suggestion, -suggestion.Score);
            }

            var result = new List<PredictiveSuggestion>();

            while (suggestions.Count > 0 && result.Count < 5)
            {
                result.Add(suggestions.Dequeue());
            }
            return result;

        }

        private static int CalculateTelemetryScore(IReadOnlyList<TelemetryDto> readings)
        {
            var score = 0;

            var recentReadings = readings.Take(10).ToList();

            foreach (var reading in recentReadings)
            {
                if (reading.Temperature >= 35)
                {
                    score += 30;
                }
                else if (reading.Temperature >= 32)
                {
                    score += 15;
                }

                if (reading.Voltage >= 240)
                {
                    score += 30;
                }
                else if (reading.Voltage >= 235)
                {
                    score += 15;
                }

                if (reading.Current >= 5)
                {
                    score += 30;
                }
                else if (reading.Current >= 4.5)
                {
                    score += 15;
                }
            }

            return score;
        }

        private static AlertSeverity DetermineSeverity(int score)
        {
            return score switch {

                >= 100 => AlertSeverity.Critical,
                >= 60 => AlertSeverity.High,
                >= 30 => AlertSeverity.Medium,
                _ => AlertSeverity.Low
            };
        }

        private static string BuildDescription(IReadOnlyList<TelemetryDto> readings, int score)
        {
            var latest = readings[0];

            var problems = new List<string>();

            if(latest.Temperature >= 35)
            {
                problems.Add($"High temperature detected: ({latest.Temperature:F1}^C)");
            }

            if (latest.Voltage >= 240)
            {
                problems.Add($"High Voltage detected: ({latest.Voltage:F1}V)");
            }

            if (latest.Current   >= 5)
            {
                problems.Add($"High Current detected: ({latest.Current:F1}A)");
            }


            if (problems.Count == 0)
            {
                problems.Add("No significant issues detected.");
            }

            return $"Recent readings say: {string.Join(", ", problems)}";
        }

        private static PredictiveSuggestion CreateAlertSuggestion(SensorAlert alert)
        {

            var score = alert.Severity switch
            {
                AlertSeverity.Critical => 100,
                AlertSeverity.High => 75,
                AlertSeverity.Medium => 50,
                AlertSeverity.Low => 25,
                _ => 0
            };

            return new PredictiveSuggestion
            {
                SensorId = alert.SensorId,

                Title = $"Alert: {alert.Message}",

                Description = $"Alert generated at {alert.Timestamp:G}. Severity: {alert.Severity}.",

                Type = SuggestionType.DeviceWarning,

                Severity = alert.Severity,

                Score = score
            };
        }

    }
}
