using Microsoft.AspNetCore.Http;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Security.Claims;

namespace SmartX.Application.Services;

public class AuditLogService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IAuditLogRepository _repository;

    public AuditLogService(
        IAuditLogRepository repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
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
            UserId = userId ?? GetCurrentUserId(),

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

    private Guid? GetCurrentUserId()
    {
        var claim =
            _httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClaimTypes.Name);

        if (claim is null)
            return null;

        return Guid.TryParse(claim.Value, out var userId)
            ? userId
            : null;
    }
}