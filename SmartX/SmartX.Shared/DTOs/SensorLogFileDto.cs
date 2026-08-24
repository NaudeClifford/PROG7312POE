namespace SmartX.Shared.DTOs;

public class SensorLogFileDto
{
    public Guid Id { get; set; }

    public Guid SensorId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "text/plain";

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; }

    public Guid UploadedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}