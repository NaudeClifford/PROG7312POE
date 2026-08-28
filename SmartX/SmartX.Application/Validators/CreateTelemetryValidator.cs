using FluentValidation;
using SmartX.Application.Requests.Telemetry;

namespace SmartX.Application.Validators;

public class CreateTelemetryValidator
    : AbstractValidator<CreateTelemetryRequest>
{
    public CreateTelemetryValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty()
            .WithMessage(
                "A valid sensor ID is required.");

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage(
                "A timestamp is required.");

        RuleFor(x => x.Voltage)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Voltage.HasValue)
            .WithMessage(
                "Voltage cannot be negative.");

        RuleFor(x => x.Current)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Current.HasValue)
            .WithMessage(
                "Current cannot be negative.");

        RuleFor(x => x.Power)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Power.HasValue)
            .WithMessage(
                "Power cannot be negative.");

        RuleFor(x => x)
            .Must(ValidateTelemetryReadings)
            .WithMessage(
                "Telemetry contains an invalid reading.");
    }

    private static bool ValidateTelemetryReadings(
        CreateTelemetryRequest request)
    {
        double?[] readings =
        {
            request.Voltage,
            request.Current,
            request.Power,
            request.Temperature
        };

        return readings.All(reading =>
            !reading.HasValue ||
            (!double.IsNaN(reading.Value) &&
             !double.IsInfinity(reading.Value)));
    }
}
