using SmartX.Shared.Mapping;
using FluentValidation;
using SmartX.Application.Requests.Sensor;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.Models;
using System.Security.Claims;

namespace SmartX.Application.Services.CRUD;

public class SensorCrudService :
ICrudService<
SensorDto,
CreateSensorRequest,
UpdateSensorRequest>
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IGatewayRepository _gatewayRepository;

    private readonly IValidator<CreateSensorRequest>
        _createValidator;

    private readonly IValidator<UpdateSensorRequest>
        _updateValidator;

    private readonly IMapper _mapper;
    private readonly AuditLogService _auditLog;

    public SensorCrudService(
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository,
        IValidator<CreateSensorRequest> createValidator,
        IValidator<UpdateSensorRequest> updateValidator,
        IMapper mapper,
        AuditLogService auditLog)
    {
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _mapper = mapper;
        _auditLog = auditLog;
    }

    public async Task<Result<IReadOnlyList<SensorDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var sensors = await _sensorRepository.GetAllAsync(
            cancellationToken);

        var accessibleSensors = new List<Sensor>();

        foreach (var sensor in sensors)
        {
            if (!sensor.GatewayId.HasValue)
                continue;

            var gateway = await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value,
                cancellationToken);

            if (gateway is not null)
            {
                accessibleSensors.Add(sensor);
            }
        }

        var dtos = _mapper.Map<List<SensorDto>>(
            accessibleSensors);

        return Result<IReadOnlyList<SensorDto>>.Ok(dtos);
    }

    public async Task<Result<SensorDto>> GetByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<SensorDto>.Fail(
                "Sensor ID is required.");
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (sensor is null)
        {
            return Result<SensorDto>.Fail(
                "Sensor not found.");
        }

        if (!await CanAccessSensorAsync(
                sensor,
                user,
                cancellationToken))
        {
            return Result<SensorDto>.Fail(
                "You do not have access to this sensor.");
        }

        var dto = _mapper.Map<SensorDto>(sensor);

        return Result<SensorDto>.Ok(dto);
    }

    public async Task<Result<IReadOnlyList<SensorDto>>> GetByGatewayIdAsync(
        Guid gatewayId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (gatewayId == Guid.Empty)
        {
            return Result<IReadOnlyList<SensorDto>>.Fail(
                "Gateway ID is required.");
        }

        var gateway = await _gatewayRepository.GetByIdAsync(
            gatewayId,
            cancellationToken);

        if (gateway is null)
        {
            return Result<IReadOnlyList<SensorDto>>.Fail(
                "Gateway not found.");
        }

        if (!CanAccessCompany(
                user,
                gateway.CompanyId))
        {
            return Result<IReadOnlyList<SensorDto>>.Fail(
                "You do not have access to this gateway.");
        }

        var sensors = await _sensorRepository.GetAllAsync(
            cancellationToken);

        var gatewaySensors = sensors
            .Where(x => x.GatewayId == gatewayId)
            .ToList();

        var dtos = _mapper.Map<List<SensorDto>>(
            gatewaySensors);

        return Result<IReadOnlyList<SensorDto>>.Ok(dtos);
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateSensorRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<Guid>.Fail(
                "Request is required.");
        }

        var validationResult =
            await _createValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors
                    .Select(x => x.ErrorMessage));

            return Result<Guid>.Fail(errors);
        }

        if (!request.GatewayId.HasValue)
        {
            return Result<Guid>.Fail(
                "Sensor must be associated with a gateway.");
        }

        var gateway = await _gatewayRepository.GetByIdAsync(
            request.GatewayId.Value,
            cancellationToken);

        if (gateway is null)
        {
            return Result<Guid>.Fail(
                "Gateway not found.");
        }

        if (!CanAccessCompany(
                user,
                gateway.CompanyId))
        {
            return Result<Guid>.Fail(
                "You do not have access to this gateway.");
        }

        var sensor = _mapper.Map<Sensor>(request);

        sensor.Id = Guid.NewGuid();
        sensor.GatewayId = request.GatewayId;
        sensor.CreatedAt = DateTime.UtcNow;
        sensor.UpdatedAt = DateTime.UtcNow;

        await _sensorRepository.AddAsync(
            sensor,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: sensor.Id,
            action: "Created",
            companyId: gateway.CompanyId,
            details: "Sensor created.",
            cancellationToken: cancellationToken);

        return Result<Guid>.Ok(sensor.Id);
    }

    public async Task<Result<bool>> UpdateAsync(
        UpdateSensorRequest request,
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
                "Sensor ID is required.");
        }

        var validationResult =
            await _updateValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join(
                "; ",
                validationResult.Errors
                    .Select(x => x.ErrorMessage));

            return Result<bool>.Fail(errors);
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (sensor is null)
        {
            return Result<bool>.Fail(
                "Sensor not found.");
        }

        if (!sensor.GatewayId.HasValue)
        {
            return Result<bool>.Fail(
                "Sensor is not associated with a gateway.");
        }

        var existingGateway =
            await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value,
                cancellationToken);

        if (existingGateway is null)
        {
            return Result<bool>.Fail(
                "Gateway not found.");
        }

        if (!CanAccessCompany(
                user,
                existingGateway.CompanyId))
        {
            return Result<bool>.Fail(
                "You do not have access to this sensor.");
        }

        if (!request.GatewayId.HasValue)
        {
            return Result<bool>.Fail(
                "Sensor must be associated with a gateway.");
        }

        var gateway = await _gatewayRepository.GetByIdAsync(
            request.GatewayId.Value,
            cancellationToken);

        if (gateway is null)
        {
            return Result<bool>.Fail(
                "Gateway not found.");
        }

        if (!CanAccessCompany(
                user,
                gateway.CompanyId))
        {
            return Result<bool>.Fail(
                "You do not have access to this gateway.");
        }

        if (gateway.CompanyId != existingGateway.CompanyId)
        {
            return Result<bool>.Fail(
                "Sensor cannot be moved to another company.");
        }

        _mapper.Map(request, sensor);

        sensor.GatewayId = request.GatewayId;
        sensor.UpdatedAt = DateTime.UtcNow;

        await _sensorRepository.UpdateAsync(
            sensor,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: sensor.Id,
            action: "Updated",
            companyId: existingGateway.CompanyId,
            details: "Sensor updated.",
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
                "Sensor ID is required.");
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (sensor is null)
        {
            return Result<bool>.Fail(
                "Sensor not found.");
        }

        if (!sensor.GatewayId.HasValue)
        {
            return Result<bool>.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gateway = await _gatewayRepository.GetByIdAsync(
            sensor.GatewayId.Value,
            cancellationToken);

        if (gateway is null)
        {
            return Result<bool>.Fail(
                "Gateway not found.");
        }

        if (!CanAccessCompany(
                user,
                gateway.CompanyId))
        {
            return Result<bool>.Fail(
                "You do not have access to this sensor.");
        }

        await _sensorRepository.DeleteAsync(
            id,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: id,
            action: "Deleted",
            companyId: gateway.CompanyId,
            details: "Sensor deleted.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    private static bool IsAdministrator(
        ClaimsPrincipal user)
    {
        return user?.IsInRole("Administrator") == true;
    }

    private static bool IsTechnician(
        ClaimsPrincipal user)
    {
        return user?.IsInRole("Technician") == true;
    }

    private static Guid? GetUserCompanyId(
        ClaimsPrincipal user)
    {
        if (user is null)
            return null;

        var claim = user.FindFirst("CompanyId")?.Value;

        if (!Guid.TryParse(
                claim,
                out var companyId))
        {
            return null;
        }

        if (companyId == Guid.Empty)
            return null;

        return companyId;
    }

    private static bool CanAccessCompany(
        ClaimsPrincipal user,
        Guid companyId)
    {
        if (user is null || companyId == Guid.Empty)
            return false;

        if (!IsAdministrator(user) &&
            !IsTechnician(user))
        {
            return false;
        }

        var userCompanyId = GetUserCompanyId(user);

        return userCompanyId.HasValue &&
               userCompanyId.Value == companyId;
    }

    private async Task<bool> CanAccessSensorAsync(
        Sensor sensor,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!sensor.GatewayId.HasValue)
            return false;

        var gateway = await _gatewayRepository.GetByIdAsync(
            sensor.GatewayId.Value,
            cancellationToken);

        if (gateway is null)
            return false;

        return CanAccessCompany(
            user,
            gateway.CompanyId);
    }

}