using SmartX.WPF.ViewModels.Base;

namespace SmartX.WPF.ViewModels.Home;

public class HomeViewModel : ViewModelBase
{
    public string Title => "SmartX";

    public string Subtitle =>
        "Smart Sensor Monitoring Platform";

    public string Description =>
        "Monitor your sensors, view live telemetry and explore historical data from one central platform.";
}