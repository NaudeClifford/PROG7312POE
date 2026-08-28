using Microsoft.AspNetCore.Http;

namespace SmartX.Application.Requests.SensorLogFile;

public class CreateSensorLogFileRequest
{
    public Guid SensorId { get; set; }

    public IFormFile? File { get; set; }
}

