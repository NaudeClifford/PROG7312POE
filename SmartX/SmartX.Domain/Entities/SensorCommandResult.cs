
namespace SmartX.Domain.Entities
{
    public class SensorCommandResult
    {

        public bool Success { get; set; }

        public string? Response { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
