using FluentValidation;
using SmartX.Application.Commands.Users;

namespace SmartX.Application.Validators.Users;

public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email address is required.");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required.");

        RuleFor(x => x.FirebaseUid)
            .NotEmpty()
            .WithMessage("Firebase UID is required.");

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Invalid user role.");
    }
}