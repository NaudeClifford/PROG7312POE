namespace SmartX.Application.Requests.Sensor;

public class SendSensorCommandRequest
{
    public Guid SensorId { get; set; }

    public string Command { get; set; } = string.Empty;
}
