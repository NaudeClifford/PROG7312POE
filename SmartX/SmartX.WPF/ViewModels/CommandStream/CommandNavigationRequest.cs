namespace SmartX.WPF.ViewModels.CommandStream;

public sealed class CommandNavigationRequest
{
    public Guid SensorId { get; init; }

    public string Command { get; init; } = string.Empty;

    public string Source { get; init; } = "Predictive recommendation";
}