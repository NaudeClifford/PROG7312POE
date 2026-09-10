using SmartX.Shared.DTOs;

namespace SmartX.WPF.Services.Demo;

public interface IGuestDemoService
{
    IReadOnlyList<GatewayDto> GetGateways();

    void Reset();
}
