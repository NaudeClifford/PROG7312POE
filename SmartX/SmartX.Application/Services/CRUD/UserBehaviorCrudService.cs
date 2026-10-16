using FluentValidation;
using SmartX.Application.Requests.UserBehavior;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Mapping;
using SmartX.Shared.Models;
using System.Security.Claims;

namespace SmartX.Application.Services.CRUD;

public sealed class UserBehaviorCrudService :
    ICrudService<
        UserBehaviorDto,
        CreateUserBehaviorRequest,
        UpdateUserBehaviorRequest>
{
    private readonly IUserBehaviorRepository _userBehaviorRepository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IValidator<CreateUserBehaviorRequest> _createValidator;
    private readonly IValidator<UpdateUserBehaviorRequest> _updateValidator;
    private readonly IMapper _mapper;
    private readonly AuditLogService _auditLog;

    public UserBehaviorCrudService(
        IUserBehaviorRepository userBehaviorRepository,
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository,
        IValidator<CreateUserBehaviorRequest> createValidator,
        IValidator<UpdateUserBehaviorRequest> updateValidator,
        IMapper mapper,
        AuditLogService auditLog)
    {
        _userBehaviorRepository = userBehaviorRepository;
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _mapper = mapper;
        _auditLog = auditLog;
    }

    public async Task<Result<IReadOnlyList<UserBehaviorDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var behaviors = await _userBehaviorRepository.GetAllAsync(
            cancellationToken);

        var dtos = _mapper.Map<List<UserBehaviorDto>>(behaviors);

        return Result<IReadOnlyList<UserBehaviorDto>>.Ok(dtos);
    }

    public async Task<Result<UserBehaviorDto>> GetByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<UserBehaviorDto>.Fail(
                "User behavior ID is required.");
        }

        var behavior = await _userBehaviorRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (behavior is null)
        {
            return Result<UserBehaviorDto>.Fail(
                "User behavior not found.");
        }

        if (!await CanAccessBehaviorAsync(
                user,
                behavior,
                cancellationToken))
        {
            return Result<UserBehaviorDto>.Fail(
                "You do not have access to this user behavior.");
        }

        var dto = _mapper.Map<UserBehaviorDto>(behavior);

        return Result<UserBehaviorDto>.Ok(dto);
    }

    public async Task<Result<IReadOnlyList<UserBehaviorDto>>> GetBySensorId(
        Guid sensorId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
        {
            return Result<IReadOnlyList<UserBehaviorDto>>.Fail(
                "Sensor ID is required.");
        }

        if (!await CanAccessSensorAsync(
                user,
                sensorId,
                cancellationToken))
        {
            return Result<IReadOnlyList<UserBehaviorDto>>.Fail(
                "You do not have access to this sensor.");
        }

        var behaviors = await _userBehaviorRepository.GetSensorByIdAsync(
            sensorId,
            cancellationToken);

        var dtos = _mapper.Map<List<UserBehaviorDto>>(behaviors);

        return Result<IReadOnlyList<UserBehaviorDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateUserBehaviorRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<Guid>.Fail(
                "Request is required.");
        }

        var validation = await _createValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validation.IsValid)
        {
            var errors = string.Join(
                "; ",
                validation.Errors.Select(error => error.ErrorMessage));

            return Result<Guid>.Fail(errors);
        }

        var userId = GetUserId(user);

        if (!userId.HasValue)
        {
            return Result<Guid>.Fail(
                "Authenticated user ID is missing or invalid.");
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            request.SensorId,
            cancellationToken);

        if (sensor is null)
        {
            return Result<Guid>.Fail(
                "Sensor not found.");
        }

        if (!await CanAccessSensorAsync(
                user,
                sensor.Id,
                cancellationToken))
        {
            return Result<Guid>.Fail(
                "You do not have access to this sensor.");
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return Result<Guid>.Fail(
                "Command is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Outcome))
        {
            return Result<Guid>.Fail(
                "Outcome is required.");
        }

        if (request.Timestamp == default)
        {
            return Result<Guid>.Fail(
                "A valid timestamp is required.");
        }

        if (!sensor.GatewayId.HasValue)
        {
            return Result<Guid>.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gateway = await _gatewayRepository.GetByIdAsync(
            sensor.GatewayId.Value,
            cancellationToken);

        if (gateway is null)
        {
            return Result<Guid>.Fail(
                "Gateway not found.");
        }

        var now = DateTime.UtcNow;

        var behavior = new UserBehavior
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            SensorId = sensor.Id,
            Command = request.Command.Trim(),
            Timestamp = request.Timestamp,
            Succeeded = request.Succeeded,
            Outcome = request.Outcome.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userBehaviorRepository.AddAsync(
            behavior,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "UserBehavior",
            entityId: behavior.Id,
            action: "Created",
            companyId: gateway.CompanyId,
            details: "User behavior recorded.",
            cancellationToken: cancellationToken);

        return Result<Guid>.Ok(behavior.Id);
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateUserBehaviorRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<bool>.Fail(
                "Request is required.");
        }

        if (request.Id == Guid.Empty)
        {
            return Result<bool>.Fail(
                "User behavior ID is required.");
        }

        var validation = await _updateValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validation.IsValid)
        {
            var errors = string.Join(
                "; ",
                validation.Errors.Select(error => error.ErrorMessage));

            return Result<bool>.Fail(errors);
        }

        var behavior = await _userBehaviorRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (behavior is null)
        {
            return Result<bool>.Fail(
                "User behavior not found.");
        }

        if (!await CanAccessBehaviorAsync(
                user,
                behavior,
                cancellationToken))
        {
            return Result<bool>.Fail(
                "You do not have access to this user behavior.");
        }

        // Preserve the original user, sensor, command, and timestamp.
        // Only update the recorded outcome.
        behavior.Succeeded = request.Succeeded;
        behavior.Outcome = request.Outcome;
        behavior.UpdatedAt = DateTime.UtcNow;

        await _userBehaviorRepository.UpdateAsync(
            behavior,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "UserBehavior",
            entityId: behavior.Id,
            action: "Updated",
            companyId: await GetSensorCompanyIdAsync(
                behavior.SensorId,
                cancellationToken),
            details: "User behavior outcome updated.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<bool>.Fail(
                "User behavior ID is required.");
        }

        var behavior = await _userBehaviorRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (behavior is null)
        {
            return Result<bool>.Fail(
                "User behavior not found.");
        }

        if (!await CanAccessBehaviorAsync(
                user,
                behavior,
                cancellationToken))
        {
            return Result<bool>.Fail(
                "You do not have access to this user behavior.");
        }

        var companyId = await GetSensorCompanyIdAsync(
            behavior.SensorId,
            cancellationToken);

        await _userBehaviorRepository.DeleteAsync(
            id,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "UserBehavior",
            entityId: id,
            action: "Deleted",
            companyId: companyId,
            details: "User behavior deleted.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    private async Task<bool> CanAccessBehaviorAsync(
        ClaimsPrincipal user,
        UserBehavior behavior,
        CancellationToken cancellationToken)
    {
        if (user is null)
            return false;

        var userId = GetUserId(user);

        // Users can access their own records only when they also
        // have permission to access the associated sensor.
        if (userId == behavior.UserId)
        {
            return await CanAccessSensorAsync(
                user,
                behavior.SensorId,
                cancellationToken);
        }

        // Administrators and technicians may access behavior records
        // for sensors within their authorized company.
        return await CanAccessSensorAsync(
            user,
            behavior.SensorId,
            cancellationToken);
    }

    private async Task<bool> CanAccessSensorAsync(
        ClaimsPrincipal user,
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        if (user is null || sensorId == Guid.Empty)
            return false;

        if (!user.IsInRole("Administrator") &&
            !user.IsInRole("Technician"))
        {
            return false;
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            sensorId,
            cancellationToken);

        if (sensor?.GatewayId is not Guid gatewayId)
            return false;

        var gateway = await _gatewayRepository.GetByIdAsync(
            gatewayId,
            cancellationToken);

        if (gateway is null)
            return false;

        var companyClaim = user.FindFirst("CompanyId")?.Value;

        return Guid.TryParse(companyClaim, out var companyId) &&
               companyId != Guid.Empty &&
               companyId == gateway.CompanyId;
    }

    private async Task<Guid> GetSensorCompanyIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        var sensor = await _sensorRepository.GetByIdAsync(
            sensorId,
            cancellationToken);

        if (sensor?.GatewayId is not Guid gatewayId)
        {
            throw new InvalidOperationException(
                "Cannot find the sensor gateway for the audit record.");
        }

        var gateway = await _gatewayRepository.GetByIdAsync(
            gatewayId,
            cancellationToken);

        if (gateway is null)
        {
            throw new InvalidOperationException(
                "Cannot find the sensor gateway for the audit record.");
        }

        return gateway.CompanyId;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var value = user?.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(value, out var userId) ||
            userId == Guid.Empty)
        {
            return null;
        }

        return userId;
    }
}