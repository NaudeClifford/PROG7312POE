namespace SmartX.Shared.Models
{
    public class TelemetryPacket<T>(T value, Guid sensorId)
    {
        public Guid SensorId { get; set; } = sensorId;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public T Value { get; set; } = value;
    }
}
