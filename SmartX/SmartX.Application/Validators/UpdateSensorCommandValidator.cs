using FluentValidation;
using SmartX.Application.Commands.Company;
using System.Text.RegularExpressions;

namespace SmartX.Application.Validators;

public class UpdateSensorCommandValidator
    : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateSensorCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Sensor ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DeviceIdentifier)
            .NotEmpty()
            .Must(IsValidDeviceIdentifier)
            .WithMessage("Enter a valid device identifier.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("A valid sensor category is required.");

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }

    private static bool IsValidMacAddress(string macAddress)
    {
        return Regex.IsMatch(
            macAddress,
            "^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$");
    }

    private static bool LooksLikeMacAddress(string identifier)
    {
        return identifier.Count(c => c == ':' || c == '-') >= 2;
    }

    private static bool IsValidGeneralIdentifier(string identifier)
    {
        return identifier.Length <= 100;
    }

    private static bool IsValidDeviceIdentifier(string identifier)
    {
        if (LooksLikeMacAddress(identifier))
        {
            return IsValidMacAddress(identifier);
        }

        return IsValidGeneralIdentifier(identifier);
    }
}