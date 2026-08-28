using FluentValidation;
using SmartX.Application.Requests.SensorLogFile;

namespace SmartX.Application.Validators.SensorLogFile;

public class CreateSensorLogFileRequestValidator
    : AbstractValidator<CreateSensorLogFileRequest>
{
    public CreateSensorLogFileRequestValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty()
            .WithMessage("Sensor ID is required.");

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("A file is required.");

        RuleFor(x => x.File)
            .Must(file =>
                file is not null &&
                file.Length > 0)
            .WithMessage("The file cannot be empty.");

        RuleFor(x => x.File)
            .Must(file =>
                file is not null &&
                string.Equals(
                    file.ContentType,
                    "text/plain",
                    StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only text files are allowed.");

        RuleFor(x => x.File)
            .Must(file =>
                file is not null &&
                Path.GetExtension(file.FileName)
                    .Equals(
                        ".txt",
                        StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .txt files are allowed.");
    }
}
