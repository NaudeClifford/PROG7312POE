namespace SmartX.Application.Requests.UserBehavior
{
    public class UpdateUserBehaviorRequest
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid UserId { get; init; }

        public Guid SensorId { get; init; }

        public string Command { get; init; } = string.Empty;

        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public bool Succeeded { get; set; }

        public string? Outcome { get; set; }
    }
}
