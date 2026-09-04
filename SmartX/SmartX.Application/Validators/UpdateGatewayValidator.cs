using FluentValidation;
using SmartX.Application.Requests.Gateway;

namespace SmartX.Application.Validators;

public class UpdateGatewayValidator
    : AbstractValidator<UpdateGatewayRequest>
{
    public UpdateGatewayValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("A valid gateway ID is required.");

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
            .When(x => !string.IsNullOrWhiteSpace(x.IpAddress))
            .WithMessage("IP address must not exceed 50 characters.");

        RuleFor(x => x.IpAddress)
    .Must(BeValidIpAddress)
    .When(x => !string.IsNullOrWhiteSpace(x.IpAddress))
    .WithMessage("Please enter a valid IP address.");
    }
    private static bool BeValidIpAddress(string? ipAddress)
    {
        return System.Net.IPAddress.TryParse(
            ipAddress,
            out _);
    }
}
