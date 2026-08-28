using AutoMapper;
using FluentValidation;
using SmartX.Application.Requests.Sensor;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs.Sensors;
using SmartX.Shared.Models;

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

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<IReadOnlyList<SensorDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var sensors =
            await _sensorRepository.GetAllAsync(
                cancellationToken);

        var dtos =
            _mapper.Map<List<SensorDto>>(sensors);

        return Result<IReadOnlyList<SensorDto>>.Ok(dtos);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<SensorDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<SensorDto>.Fail(
                "Sensor ID is required.");
        }

        var sensor =
            await _sensorRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (sensor is null)
        {
            return Result<SensorDto>.Fail(
                "Sensor not found.");
        }

        var dto =
            _mapper.Map<SensorDto>(sensor);

        return Result<SensorDto>.Ok(dto);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<Result<Guid>> CreateAsync(
        CreateSensorRequest request,
        CancellationToken cancellationToken = default)
    {
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

        // -----------------------------------------------------
        // GATEWAY VALIDATION
        // -----------------------------------------------------

        if (!request.GatewayId.HasValue)
        {
            return Result<Guid>.Fail(
                "Sensor must be associated with a gateway.");
        }

        var gateway =
            await _gatewayRepository.GetByIdAsync(
                request.GatewayId.Value,
                cancellationToken);

        if (gateway is null)
        {
            return Result<Guid>.Fail(
                "Gateway not found.");
        }

        // -----------------------------------------------------
        // CREATE SENSOR
        // -----------------------------------------------------

        var now = DateTime.UtcNow;

        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),

            Name = request.Name,
            DeviceIdentifier = request.DeviceIdentifier,
            Category = request.Category,
            Location = request.Location,
            Description = request.Description,

            GatewayId = request.GatewayId,

            IsActive = request.IsActive,

            CreatedAt = now,
            UpdatedAt = now
        };

        await _sensorRepository.AddAsync(
            sensor,
            cancellationToken);

        // -----------------------------------------------------
        // AUDIT
        // -----------------------------------------------------

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: sensor.Id,
            action: "Created",
            companyId: gateway.CompanyId,
            details: "Sensor created.",
            cancellationToken: cancellationToken);

        return Result<Guid>.Ok(sensor.Id);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result<bool>> UpdateAsync(
        UpdateSensorRequest request,
        CancellationToken cancellationToken = default)
    {
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

        var sensor =
            await _sensorRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

        if (sensor is null)
        {
            return Result<bool>.Fail(
                "Sensor not found.");
        }

        // -----------------------------------------------------
        // GATEWAY VALIDATION
        // -----------------------------------------------------

        if (!request.GatewayId.HasValue)
        {
            return Result<bool>.Fail(
                "Sensor must be associated with a gateway.");
        }

        var gateway =
            await _gatewayRepository.GetByIdAsync(
                request.GatewayId.Value,
                cancellationToken);

        if (gateway is null)
        {
            return Result<bool>.Fail(
                "Gateway not found.");
        }

        // -----------------------------------------------------
        // UPDATE SENSOR
        // -----------------------------------------------------

        sensor.Name =
            request.Name;

        sensor.DeviceIdentifier =
            request.DeviceIdentifier;

        sensor.Category =
            request.Category;

        sensor.Location =
            request.Location;

        sensor.Description =
            request.Description;

        sensor.GatewayId =
            request.GatewayId;

        sensor.IsActive =
            request.IsActive;

        // Preserve CreatedAt.
        // Update UpdatedAt for synchronization.
        sensor.UpdatedAt =
            DateTime.UtcNow;

        await _sensorRepository.UpdateAsync(
            sensor,
            cancellationToken);

        // -----------------------------------------------------
        // AUDIT
        // -----------------------------------------------------

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: sensor.Id,
            action: "Updated",
            companyId: gateway.CompanyId,
            details: "Sensor updated.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<bool>.Fail(
                "Sensor ID is required.");
        }

        var sensor =
            await _sensorRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (sensor is null)
        {
            return Result<bool>.Fail(
                "Sensor not found.");
        }

        // -----------------------------------------------------
        // GATEWAY / COMPANY FOR AUDIT
        // -----------------------------------------------------

        if (!sensor.GatewayId.HasValue)
        {
            return Result<bool>.Fail(
                "Sensor is not associated with a gateway.");
        }

        var gateway =
            await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value,
                cancellationToken);

        if (gateway is null)
        {
            return Result<bool>.Fail(
                "Gateway not found.");
        }

        // -----------------------------------------------------
        // DELETE
        // -----------------------------------------------------

        await _sensorRepository.DeleteAsync(
            id,
            cancellationToken);

        // -----------------------------------------------------
        // AUDIT
        // -----------------------------------------------------

        await _auditLog.LogAsync(
            entityType: "Sensor",
            entityId: id,
            action: "Deleted",
            companyId: gateway.CompanyId,
            details: "Sensor deleted.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }
}
