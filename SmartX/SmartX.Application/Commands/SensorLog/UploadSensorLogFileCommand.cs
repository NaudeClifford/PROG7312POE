namespace SmartX.Application.Commands.SensorLog;

public sealed class UploadSensorLogFileCommand
{
    public Guid SensorId { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public Stream Content { get; init; } = Stream.Null;
}