using FluentValidation;
using SmartX.Application.Requests.Gateway;

namespace SmartX.Application.Validators;

public class CreateGatewayValidator
    : AbstractValidator<CreateGatewayRequest>
{
    public CreateGatewayValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage("A valid company ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage(
                "Gateway name is required and must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage(
                "Gateway description must not exceed 500 characters.");

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100)
            .WithMessage(
                "Serial number must not exceed 100 characters.");

        RuleFor(x => x.IpAddress)
            .MaximumLength(50)
            .WithMessage(
                "IP address must not exceed 50 characters.");
    }
}
