
using FluentValidation;
using SmartX.Application.Requests.UserBehavior;

namespace SmartX.Application.Validators;

public sealed class UpdateUserBehaviorValidator
    : AbstractValidator<UpdateUserBehaviorRequest>
{
    public UpdateUserBehaviorValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("User behavior ID is required.");

        RuleFor(x => x.Command)
            .NotEmpty()
            .WithMessage("Command is required.")
            .MaximumLength(500)
            .WithMessage("Command cannot exceed 500 characters.");

        RuleFor(x => x.Outcome)
            .MaximumLength(2000)
            .When(x => x.Outcome is not null)
            .WithMessage("Outcome cannot exceed 2000 characters.");

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage("Timestamp is required.");
    }
}
