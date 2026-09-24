namespace SmartX.Application.Services.Sensors;

public sealed class SensorCommandResult
{
    public bool Success { get; init; }

    public string? Response { get; init; }

    public string? InverseCommand { get; init; }

    public string? ErrorMessage { get; init; }

    public static SensorCommandResult Ok(
        string? response,
        string? inverseCommand)
    {
        return new SensorCommandResult
        {
            Success = true,
            Response = response,
            InverseCommand = inverseCommand
        };
    }

    public static SensorCommandResult Fail(
        string errorMessage)
    {
        return new SensorCommandResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
