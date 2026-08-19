using FluentValidation;
using SmartX.Application.Commands.Company;

namespace SmartX.Application.Validators;

public class CreateCompanyCommandValidator
    : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}