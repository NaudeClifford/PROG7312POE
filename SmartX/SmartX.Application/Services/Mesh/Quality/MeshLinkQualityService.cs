
using SmartX.Application.Interfaces.Mesh;
using SmartX.Application.Services.Mesh.Monitoring;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Enums.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Quality;

public class MeshLinkQualityService : IMeshLinkQualityService
{
    private readonly IMeshLinkRepository _linkRepository;
    private readonly IMeshLogRepository _logRepository;
    private readonly MeshMonitoringOptions _options;
    private const double SignalWeight = 0.5;
    private const double LatencyWeight = 0.3;
    private const double EnergyWeight = 0.2;

    public MeshLinkQualityService(
        IMeshLinkRepository linkRepository,
        IMeshLogRepository logRepository,
        MeshMonitoringOptions options)
    {
        _linkRepository = linkRepository;
        _logRepository = logRepository;
        _options = options;
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

        var now = DateTime.UtcNow;

        var quality = CalculateLinkQuality(
            link.SignalStrengthDbm,
            link.LatencyMilliseconds,
            link.EnergyCost);

        var isRecent =
            link.LastSeenAt >= now.AddMinutes(
                -_options.LinkTimeoutMinutes);

        var isAvailable =
            quality >= _options.MinimumLinkQuality &&
            isRecent;

        if (previousAvailability != isAvailable)
        {
            link.IsAvailable = isAvailable;
            link.UpdatedAt = now;

            await _linkRepository.UpdateAsync(
                link,
                cancellationToken);

            // Log availability transition.
        }

        await _linkRepository.UpdateAsync(
            link,
            cancellationToken);

        if (previousAvailability != isAvailable)
        {
            await _logRepository.AddAsync(
                new MeshLog
                {
                    Id = Guid.NewGuid(),
                    GatewayId = link.GatewayId,
                    ConnectionId = link.Id,
                    EventType = "LinkAvailabilityChanged",
                    Message = isAvailable
                        ? "Mesh link became available."
                        : "Mesh link became unavailable.",
                    Severity = isAvailable
                        ? MeshLogSeverity.Information
                        : MeshLogSeverity.Warning,
                    Details = $"Link quality: {quality:F2}%. " +
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
            return 0;

        if (!double.IsFinite(signalStrengthDbm.Value) ||
            !double.IsFinite(latencyMilliseconds.Value) ||
            !double.IsFinite(energyCost.Value))
            return 0;

        if (latencyMilliseconds.Value < 0 ||
            energyCost.Value < 0)
            return 0;

        var signalQuality = CalculateSignalQuality(
            signalStrengthDbm.Value);

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
        if (!double.IsFinite(linkQuality) ||
            !double.IsFinite(latencyMilliseconds) ||
            !double.IsFinite(energyCost) ||
            latencyMilliseconds < 0 ||
            energyCost < 0)
        {
            return double.MaxValue;
        }

        return
            (100 - Math.Clamp(linkQuality, 0, 100)) * 0.5 +
            latencyMilliseconds * 0.3 +
            energyCost * 0.2;
    }

    private static double CalculateSignalQuality(double signal)
    {
        return Math.Clamp(
            (signal + 100) * 2,
            0,
            100);
    }

    private double CalculateLatencyQuality(double latency)
    {
        return Math.Clamp(
            100 * (1 - latency /
                _options.MaximumLatencyMilliseconds),
            0,
            100);
    }

    private double CalculateEnergyQuality(double energy)
    {
        return Math.Clamp(
            100 * (1 - energy /
                _options.MaximumEnergyCost),
            0,
            100);
    }
}
