using FluentValidation;
using SmartX.Application.Requests.Company;

namespace SmartX.Application.Validators.Company;

public class UpdateCompanyValidator
    : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("A valid company ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150)
            .WithMessage(
                "Company name is required and must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage(
                "Company description must not exceed 500 characters.");
    }
}
