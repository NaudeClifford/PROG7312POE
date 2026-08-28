using AutoMapper;
using FluentValidation;
using SmartX.Application.Requests.SensorLogFile;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;
using SmartX.Shared.Models;

namespace SmartX.Application.Services.CRUD;

public class SensorLogFileCrudService
{
    private readonly IGatewayRepository _gatewayRepository;
    private readonly ISensorLogFileRepository _repository;
    private readonly ISensorRepository _sensorRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateSensorLogFileRequest> _createValidator;
    private readonly AuditLogService _auditLog;

    private readonly string _storagePath;

    public SensorLogFileCrudService(
        ISensorLogFileRepository repository,
        ISensorRepository sensorRepository,
        IGatewayRepository gatewayRepository,
        IMapper mapper,
        IValidator<CreateSensorLogFileRequest> createValidator,
        AuditLogService auditLog)
    {
        _repository = repository;
        _sensorRepository = sensorRepository;
        _gatewayRepository = gatewayRepository;
        _mapper = mapper;
        _createValidator = createValidator;
        _auditLog = auditLog;

        _storagePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Local",
            "SensorLogs");
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<IReadOnlyList<SensorLogFileDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var logs = await _repository.GetAllAsync(
            cancellationToken);

        var dtos =
            _mapper.Map<List<SensorLogFileDto>>(logs);

        return Result<IReadOnlyList<SensorLogFileDto>>.Ok(dtos);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<SensorLogFileDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result<SensorLogFileDto>.Fail(
                "Sensor log file ID is required.");
        }

        var log = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (log is null)
        {
            return Result<SensorLogFileDto>.Fail(
                "Sensor log file not found.");
        }

        return Result<SensorLogFileDto>.Ok(
            _mapper.Map<SensorLogFileDto>(log));
    }

    // =========================================================
    // GET BY SENSOR
    // =========================================================

    public async Task<Result<IReadOnlyList<SensorLogFileDto>>>
        GetBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
        {
            return Result<IReadOnlyList<SensorLogFileDto>>.Fail(
                "Sensor ID is required.");
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            sensorId,
            cancellationToken);

        if (sensor is null)
        {
            return Result<IReadOnlyList<SensorLogFileDto>>.Fail(
                "Sensor not found.");
        }

        var logs = await _repository.GetBySensorIdAsync(
            sensorId,
            cancellationToken);

        var dtos =
            _mapper.Map<List<SensorLogFileDto>>(logs);

        return Result<IReadOnlyList<SensorLogFileDto>>.Ok(dtos);
    }

    // =========================================================
    // UPLOAD / CREATE
    // =========================================================

    public async Task<Result<Guid>> CreateAsync(
        CreateSensorLogFileRequest request,
        Guid uploadedByUserId,
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
                validationResult.Errors.Select(
                    x => x.ErrorMessage));

            return Result<Guid>.Fail(errors);
        }

        if (uploadedByUserId == Guid.Empty)
        {
            return Result<Guid>.Fail(
                "Uploaded by user ID is required.");
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            request.SensorId,
            cancellationToken);

        if (sensor is null)
        {
            return Result<Guid>.Fail(
                "Sensor not found.");
        }

        var file = request.File!;

        Directory.CreateDirectory(
            _storagePath);

        var id = Guid.NewGuid();

        var storedFileName =
            $"{id}.txt";

        var storedPath =
            Path.Combine(
                _storagePath,
                storedFileName);

        try
        {
            await using var input =
                file.OpenReadStream();

            await using var output =
                File.Create(storedPath);

            await input.CopyToAsync(
                output,
                cancellationToken);
        }
        catch
        {
            if (File.Exists(storedPath))
                File.Delete(storedPath);

            throw;
        }

        var fileInfo =
            new FileInfo(storedPath);

        var now =
            DateTime.UtcNow;

        var entity = new SensorLogFile
        {
            Id = id,

            SensorId = request.SensorId,

            FileName =
                Path.GetFileName(file.FileName),

            ContentType =
                "text/plain",

            FileSize =
                fileInfo.Length,

            UploadedAt =
                now,

            UploadedByUserId =
                uploadedByUserId,

            CreatedAt =
                now,

            UpdatedAt =
                now
        };

        await _repository.AddAsync(
            entity,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "SensorLogFile",
            entityId: entity.Id,
            action: "Created",
            companyId: await GetSensorCompanyIdAsync(
                sensor,
                cancellationToken),
            userId: uploadedByUserId,
            details: "Sensor log file uploaded.",
            cancellationToken: cancellationToken);

        return Result<Guid>.Ok(entity.Id);
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
                "Sensor log file ID is required.");
        }

        var log = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (log is null)
        {
            return Result<bool>.Fail(
                "Sensor log file not found.");
        }

        var sensor = await _sensorRepository.GetByIdAsync(
            log.SensorId,
            cancellationToken);

        if (sensor is null)
        {
            return Result<bool>.Fail(
                "Sensor not found.");
        }

        var storedPath =
            Path.Combine(
                _storagePath,
                $"{log.Id}.txt");

        if (File.Exists(storedPath))
        {
            File.Delete(storedPath);
        }

        await _repository.DeleteAsync(
            id,
            cancellationToken);

        await _auditLog.LogAsync(
            entityType: "SensorLogFile",
            entityId: id,
            action: "Deleted",
            companyId: await GetSensorCompanyIdAsync(
                sensor,
                cancellationToken),
            details: "Sensor log file deleted.",
            cancellationToken: cancellationToken);

        return Result<bool>.Ok(true);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private async Task<Guid> GetSensorCompanyIdAsync(
        Sensor sensor,
        CancellationToken cancellationToken)
    {
        if (!sensor.GatewayId.HasValue)
            return Guid.Empty;

        var gateway =
            await _gatewayRepository.GetByIdAsync(
                sensor.GatewayId.Value,
                cancellationToken);

        return gateway?.CompanyId ?? Guid.Empty;
    }

}
