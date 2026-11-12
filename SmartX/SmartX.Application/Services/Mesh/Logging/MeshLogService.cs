using SmartX.Application.Interfaces.Mesh;
using SmartX.Domain.Entities.Mesh;
using SmartX.Domain.Interfaces.Mesh;

namespace SmartX.Application.Services.Mesh.Logging;

public class MeshLogService(
    IMeshLogRepository logRepository) : IMeshLogService
{
    private readonly IMeshLogRepository _logRepository =
        logRepository;

    public async Task LogAsync(
        MeshLog log,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);

        if (log.Id == Guid.Empty)
            log.Id = Guid.NewGuid();

        if (log.Timestamp == default)
            log.Timestamp = DateTime.UtcNow;

        await _logRepository.AddAsync(
            log,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MeshLog>> GetByNodeIdAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        if (nodeId == Guid.Empty)
            return [];

        return await _logRepository.GetByNodeIdAsync(
            nodeId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MeshLog>> GetByGatewayIdAsync(
        Guid gatewayId,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
            return [];

        return await _logRepository.GetByGatewayIdAsync(
            gatewayId,
            cancellationToken);
    }

    public async Task DeleteOlderThanAsync(
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        if (cutoff == default)
            throw new ArgumentException(
                "Cutoff date cannot be empty.",
                nameof(cutoff));

        await _logRepository.DeleteOlderThanAsync(
            cutoff,
            cancellationToken);
    }
}