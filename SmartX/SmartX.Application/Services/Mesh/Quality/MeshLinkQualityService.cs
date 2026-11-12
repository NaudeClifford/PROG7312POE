using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Quality;

public class MeshLinkQualityService : IMeshLinkQualityService
{
    private readonly IMeshLinkRepository _linkRepository;
    private readonly IMeshLogRepository _logRepository;

    private const double SignalWeight = 0.5;
    private const double LatencyWeight = 0.3;
    private const double EnergyWeight = 0.2;

    public MeshLinkQualityService(
        IMeshLinkRepository linkRepository,
        IMeshLogRepository logRepository)
    {
        _linkRepository = linkRepository;
        _logRepository = logRepository;
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

            await EvaluateLinkInternalAsync(
                link,
                cancellationToken);
        }
    }

    public async Task EvaluateLinkAsync(
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        if (linkId == Guid.Empty)
            throw new ArgumentException(
                "A valid link ID is required.",
                nameof(linkId));

        var link = await _linkRepository.GetByIdAsync(
            linkId,
            cancellationToken);

        if (link is null)
            return;

        await EvaluateLinkInternalAsync(
            link,
            cancellationToken);
    }

    private async Task EvaluateLinkInternalAsync(
        MeshLink link,
        CancellationToken cancellationToken)
    {
        var previousAvailability = link.IsAvailable;

        var previousSignal = link.SignalStrengthDbm;
        var previousLatency = link.LatencyMilliseconds;
        var previousEnergy = link.EnergyCost;

        var quality = CalculateLinkQuality(
            link.SignalStrengthDbm,
            link.LatencyMilliseconds,
            link.EnergyCost);

        var isAvailable =
            quality >= 20 &&
            link.LastSeenAt >=
            DateTime.UtcNow.AddMinutes(-2);

        link.IsAvailable = isAvailable;
        link.UpdatedAt = DateTime.UtcNow;

        await _linkRepository.UpdateAsync(
            link,
            cancellationToken);

        if (previousAvailability != link.IsAvailable)
        {
            await _logRepository.AddAsync(
                new MeshLog
                {
                    Id = Guid.NewGuid(),
                    GatewayId = link.GatewayId,
                    ConnectionId = link.Id,
                    EventType = "LinkAvailabilityChanged",
                    Message = link.IsAvailable
                        ? "Mesh link became available."
                        : "Mesh link became unavailable.",
                    Severity = link.IsAvailable
                        ? MeshLogSeverity.Information
                        : MeshLogSeverity.Warning,
                    Details =
                        $"Link quality: {quality:F2}%. " +
                        $"Previous availability: {previousAvailability}.",
                    Timestamp = DateTime.UtcNow
                },
                cancellationToken);
        }
    }

    public double CalculateLinkQuality(
        double? signalStrengthDbm,
        double? latencyMilliseconds,
        double? energyCost)
    {
        if (!signalStrengthDbm.HasValue ||
            !latencyMilliseconds.HasValue ||
            !energyCost.HasValue)
        {
            return 0;
        }

        var signalQuality = Math.Clamp(
            (signalStrengthDbm.Value + 100) * 2,
            0,
            100);

        var latencyQuality = CalculateLatencyQuality(
            latencyMilliseconds.Value);

        var energyQuality = CalculateEnergyQuality(
            energyCost.Value);

        return Math.Clamp(
            signalQuality * SignalWeight +
            latencyQuality * LatencyWeight +
            energyQuality * EnergyWeight,
            0,
            100);
    }

    public double CalculateRoutingCost(
        double linkQuality,
        double latencyMilliseconds,
        double energyCost)
    {
        return
            (100 - Math.Clamp(linkQuality, 0, 100)) * 0.5 +
            latencyMilliseconds * 0.3 +
            energyCost * 0.2;
    }

    private static double CalculateLatencyQuality(
        double latency)
    {
        if (!double.IsFinite(latency) || latency < 0)
            return 0;

        return Math.Clamp(
            100 - latency,
            0,
            100);
    }

    private static double CalculateEnergyQuality(
        double energy)
    {
        if (!double.IsFinite(energy) || energy < 0)
            return 0;

        return Math.Clamp(
            100 - energy,
            0,
            100);
    }
}