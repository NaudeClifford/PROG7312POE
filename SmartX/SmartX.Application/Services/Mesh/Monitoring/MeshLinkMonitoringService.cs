
using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Monitoring;

public class MeshLinkMonitoringService : IMeshLinkMonitoringService
{
    private readonly IMeshLinkRepository _linkRepository;
    private readonly IMeshLinkQualityService _qualityService;

    public MeshLinkMonitoringService(
        IMeshLinkRepository linkRepository,
        IMeshLinkQualityService qualityService)
    {
        _linkRepository = linkRepository;
        _qualityService = qualityService;
    }

    public async Task EvaluateGatewayLinksAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            throw new ArgumentException(
                "A valid gateway ID is required.",
                nameof(gatewayId));

        var links = await _linkRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);

        foreach (var link in links)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _qualityService.EvaluateLinkAsync(
                link.Id,
                cancellationToken);
        }
    }
}
