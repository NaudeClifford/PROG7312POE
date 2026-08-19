using FluentValidation;
using SmartX.Application.Commands.Company;

namespace SmartX.Application.Validators;

public class UpdateCompanyCommandValidator
    : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}