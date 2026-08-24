using SmartX.Shared.DTOs;

public class SensorLogFileUploadResultDto
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public SensorLogFileDto? Data { get; set; }
}

