namespace SmartX.Application.Requests.Gateway;

public class CreateGatewayRequest
{
    public Guid CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public string? IpAddress { get; set; }
}
