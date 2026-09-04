using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.WPF.ViewModels.Telemetry;

public class TelemetryReading
{
    public DomainTelemetry Data { get; }

    public TelemetryReading(DomainTelemetry data)
    {
        Data = data;
    }

    public static bool operator >(
        TelemetryReading left,
        TelemetryReading right)
    {
        return left.Data.Timestamp >
               right.Data.Timestamp;
    }

    public static bool operator <(
        TelemetryReading left,
        TelemetryReading right)
    {
        return left.Data.Timestamp <
               right.Data.Timestamp;
    }
}