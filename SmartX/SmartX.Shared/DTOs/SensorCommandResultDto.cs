namespace SmartX.Shared.DTOs;

public class SensorCommandResultDto
{
    public bool Success { get; set; }

    public string? Response { get; set; }

    public string? ErrorMessage { get; set; }

    public string? InverseCommand { get; set; }
}