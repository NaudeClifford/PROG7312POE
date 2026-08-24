using SmartX.Domain.Entities;

namespace SmartX.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        AuditLog log,
        CancellationToken cancellationToken = default);
}