namespace SmartX.Shared.DTOs
{
    public class UserBehaviorDto
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid UserId { get; init; }

        public Guid SensorId { get; init; }

        public string Command { get; init; } = string.Empty;

        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public bool Succeeded { get; set; }

        public string? Outcome { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
