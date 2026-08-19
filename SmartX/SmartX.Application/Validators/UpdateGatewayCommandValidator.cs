using FluentValidation;
using SmartX.Application.Commands.Gateway;

namespace SmartX.Application.Validators;

public class UpdateGatewayCommandValidator
    : AbstractValidator<UpdateGatewayCommand>
{
    public UpdateGatewayCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100);

        RuleFor(x => x.IpAddress)
            .MaximumLength(50);

        RuleFor(x => x.CompanyId)
            .NotEmpty();
    }
}