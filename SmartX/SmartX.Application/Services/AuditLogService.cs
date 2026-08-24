using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;

namespace SmartX.Application.Services;

public class AuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(
        IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid companyId,
        Guid? userId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var log = new AuditLog
        {
            Id = Guid.NewGuid(),

            CompanyId = companyId,
            UserId = userId,

            EntityType = entityType,
            EntityId = entityId,

            Action = action,

            Description = details ?? string.Empty,

            CreatedAt = now,
            UpdatedAt = now
        };

        return _repository.AddAsync(
            log,
            cancellationToken);
    }
}