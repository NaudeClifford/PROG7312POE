using SmartX.Domain.Enums;

namespace SmartX.Application.Commands.History
{
    public sealed class DeviceCommand
    {
        public Guid Id { get; init; }

        public Guid SensorId { get; init; }

        public string Name { get; init; } = string.Empty;

        public CommandPriority Priority { get; init; }

        public DateTime CreatedAt { get; init; }

        public Dictionary<string, string> Parameters { get; init; }
            = [];
    }

}
