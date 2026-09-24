using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.WPF.ViewModels.History
{
    public sealed class TelemetryGraphPoint
    {
        public DateTime Timestamp { get; init; }

        public double Power { get; init; }

        public double Temperature { get; init; }

        public double Voltage { get; init; }

        public double Current { get; init; }
    }

}
