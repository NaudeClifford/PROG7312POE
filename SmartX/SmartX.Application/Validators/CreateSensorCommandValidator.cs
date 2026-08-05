using FluentValidation;
using SmartX.Application.Commands.Sensors;

namespace SmartX.Application.Validators;

public class CreateSensorCommandValidator : AbstractValidator<CreateSensorCommand>
{
    public CreateSensorCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}