namespace SmartX.Shared.DTOs.SensorLog;

public class SensorLogFileUploadResultDto
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public Guid? LogId { get; set; }
}