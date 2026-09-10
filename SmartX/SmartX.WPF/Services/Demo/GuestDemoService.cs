using SmartX.Shared.DTOs;

namespace SmartX.WPF.Services.Demo;

public class GuestDemoService : IGuestDemoService
{
    private readonly List<GatewayDto> _gateways = [];

    public IReadOnlyList<GatewayDto> GetGateways()
    {
        if (_gateways.Count == 0)
        {
            _gateways.Add(
                new GatewayDto
                {
                    Id = Guid.NewGuid(),
                    CompanyId = Guid.NewGuid(),
                    Name = "Demo Gateway",
                    Description = "SmartX demonstration gateway",
                    SerialNumber = "DEMO-GATEWAY-001",
                    IpAddress = "192.168.1.100",
                    IsActive = true
                });
        }

        return _gateways;
    }

    public void Reset()
    {
        _gateways.Clear();
    }
}
