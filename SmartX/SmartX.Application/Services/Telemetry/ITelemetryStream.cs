using DomainTelemetry = SmartX.Domain.Entities.Telemetry;

namespace SmartX.Application.Services.Telemetry
{
    public interface ITelemetryStream
    {
        void Enqueue(DomainTelemetry telemetry);

        bool TryDequeue(out DomainTelemetry? telemetry);

        int Count { get; }

        void Clear();
    }
}
