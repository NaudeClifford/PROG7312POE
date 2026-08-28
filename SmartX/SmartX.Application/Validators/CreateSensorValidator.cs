using FluentValidation;
using SmartX.Application.Requests.Sensor;
using System.Text.RegularExpressions;

namespace SmartX.Application.Validators.Sensor;

public class CreateSensorValidator
    : AbstractValidator<CreateSensorRequest>
{
    public CreateSensorValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Sensor name is required and must not exceed 100 characters.");

        RuleFor(x => x.DeviceIdentifier)
            .NotEmpty()
            .Must(IsValidDeviceIdentifier)
            .WithMessage("Enter a valid device identifier.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Sensor location is required and must not exceed 200 characters.");

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("A valid sensor category is required.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters.");
    }

    private static bool IsValidMacAddress(string macAddress)
    {
        return Regex.IsMatch(
            macAddress,
            "^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$");
    }

    private static bool LooksLikeMacAddress(string identifier)
    {
        return identifier.Count(
            c => c == ':' || c == '-') >= 2;
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
