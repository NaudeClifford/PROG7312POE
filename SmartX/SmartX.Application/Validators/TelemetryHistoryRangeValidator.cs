using FluentValidation;
using SmartX.Application.Requests.Telemetry;

namespace SmartX.Application.Validators;

public class TelemetryHistoryRangeValidator
    : AbstractValidator<TelemetryHistoryRangeRequest>
{
    public TelemetryHistoryRangeValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty();

        RuleFor(x => x.From)
            .NotEmpty();

        RuleFor(x => x.To)
            .NotEmpty()
            .GreaterThan(x => x.From)
            .WithMessage(
                "End date must be later than start date.");

        RuleFor(x => x.From)
            .Must(x => x.Kind == DateTimeKind.Utc)
            .WithMessage("Start date must be UTC.");

        RuleFor(x => x.To)
            .Must(x => x.Kind == DateTimeKind.Utc)
            .WithMessage("End date must be UTC.");
    }
}