using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WPF.ViewModels.History
{
    public sealed class SensorCommand
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public Guid SensorId { get; set; }
        public DateTime Timestamp { get; init; } = DateTime.Now;

        public string Command { get; init; } = string.Empty;

        public string? InverseCommand { get; init; } = string.Empty;


        public string? Response { get; init; }
    }

}
