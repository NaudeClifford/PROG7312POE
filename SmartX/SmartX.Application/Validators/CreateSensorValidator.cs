using FluentValidation;
using SmartX.Application.Requests.Sensor;
using SmartX.Domain.Enums;
using System.Net;
using System.Text.RegularExpressions;

namespace SmartX.Application.Validators.Sensor;

public class CreateSensorValidator
    : AbstractValidator<CreateSensorRequest>
{
    public CreateSensorValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DeviceIdentifier)
            .NotEmpty()
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .Must(IsValidDeviceIdentifier)
            .WithMessage("Enter a valid device identifier.");

        RuleFor(x => x.IpAddress)
    .Cascade(CascadeMode.Stop)
    .Must(ip =>
        string.IsNullOrWhiteSpace(ip) ||
        IPAddress.TryParse(ip, out _))
    .WithMessage("Please enter a valid IP address.");

        RuleFor(x => x.Port)
    .Must(port =>
        !port.HasValue ||
        port.Value is >= 1 and <= 65535)
    .WithMessage("Please enter a valid port between 1 and 65535.");

        RuleFor(x => x)
     .Must(x =>
         x.IsSimulation ||
         x.Port.HasValue)
     .WithMessage("Real sensors require a valid port.");

        RuleFor(x => x.Protocol)
            .IsInEnum();

        RuleFor(x => x)
            .Must(x =>
                !x.IsSimulation ||
                x.Protocol == SensorProtocol.Simulation)
            .WithMessage(
                "Simulation sensors must use the Simulation protocol.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(200);

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
