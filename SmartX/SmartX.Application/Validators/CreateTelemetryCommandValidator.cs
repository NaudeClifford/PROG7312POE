using FluentValidation;
using SmartX.Application.Commands.Telemetry;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Application.Validators
{
    public class CreateTelemetryCommandValidator : AbstractValidator<CreateTelemetryCommand> 
    {
        public CreateTelemetryCommandValidator() {

            RuleFor(x => x.SensorId)
                .NotEmpty()
                .WithMessage("A valid sensor ID is required");

            RuleFor(x => x.TimeStamp)
                .NotEmpty()
                .WithMessage("A timestamp is required");
            
            RuleFor(x => x.Voltage)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Voltage.HasValue)
                .WithMessage("Voltage cannot be negative");
            
            RuleFor(x => x.Current)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Current.HasValue)
                .WithMessage("Current cannot be negative");

            RuleFor(x => x.Power)
                .GreaterThanOrEqualTo(0)
                .When(x => x.Power.HasValue)
                .WithMessage("Power cannot be negative");
        }
    }
}
