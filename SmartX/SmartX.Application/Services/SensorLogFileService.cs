using AutoMapper;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Shared.DTOs;

namespace SmartX.Application.Services;

public class SensorLogFileService
{
    private readonly ISensorLogFileRepository _repository;
    private readonly IMapper _mapper;

    private readonly string _storagePath;

    public SensorLogFileService(
        ISensorLogFileRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;

        _storagePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Local",
            "SensorLogs");
    }

    // =========================================================
    // GET BY SENSOR
    // =========================================================

    public async Task<IReadOnlyList<SensorLogFileDto>>
        GetBySensorIdAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
    {
        var logs =
            await _repository.GetBySensorIdAsync(
                sensorId,
                cancellationToken);

        return logs
            .Select(x => _mapper.Map<SensorLogFileDto>(x))
            .ToList();
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<SensorLogFileDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var log =
            await _repository.GetByIdAsync(
                id,
                cancellationToken);

        if (log is null)
            return null;

        return _mapper.Map<SensorLogFileDto>(log);
    }

    // =========================================================
    // UPLOAD
    // =========================================================

    public async Task<SensorLogFileDto> UploadAsync(
        Guid sensorId,
        string fileName,
        Stream fileStream,
        string contentType,
        Guid uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        // -----------------------------------------------------
        // BASIC VALIDATION
        // -----------------------------------------------------

        if (sensorId == Guid.Empty)
            throw new ArgumentException(
                "Sensor ID is required.",
                nameof(sensorId));

        if (uploadedByUserId == Guid.Empty)
            throw new ArgumentException(
                "Uploaded by user ID is required.",
                nameof(uploadedByUserId));

        if (fileStream is null)
            throw new ArgumentNullException(
                nameof(fileStream));

        if (!fileStream.CanRead)
            throw new InvalidOperationException(
                "The supplied file cannot be read.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(
                "File name is required.",
                nameof(fileName));

        // -----------------------------------------------------
        // FILE VALIDATION
        // -----------------------------------------------------

        if (!string.Equals(
                contentType,
                "text/plain",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Only text files are supported.");
        }

        var extension =
            Path.GetExtension(fileName);

        if (!string.Equals(
                extension,
                ".txt",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Only .txt files are supported.");
        }

        // -----------------------------------------------------
        // STORAGE DIRECTORY
        // -----------------------------------------------------

        Directory.CreateDirectory(
            _storagePath);

        // -----------------------------------------------------
        // GENERATE SERVER-SIDE ID
        // -----------------------------------------------------

        var id = Guid.NewGuid();

        var storedFileName =
            $"{id}.txt";

        var storedPath =
            Path.Combine(
                _storagePath,
                storedFileName);

        // -----------------------------------------------------
        // SAVE FILE
        // -----------------------------------------------------

        await using (var output =
            File.Create(storedPath))
        {
            await fileStream.CopyToAsync(
                output,
                cancellationToken);
        }

        // -----------------------------------------------------
        // FILE METADATA
        // -----------------------------------------------------

        var fileInfo =
            new FileInfo(storedPath);

        var now =
            DateTime.UtcNow;

        // -----------------------------------------------------
        // CREATE ENTITY
        // -----------------------------------------------------

        var entity = new SensorLogFile
        {
            Id = id,

            SensorId = sensorId,

            FileName =
                Path.GetFileName(fileName),

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

        // -----------------------------------------------------
        // SAVE METADATA TO JSON REPOSITORY
        // -----------------------------------------------------

        await _repository.AddAsync(
            entity,
            cancellationToken);

        // -----------------------------------------------------
        // RETURN DTO
        // -----------------------------------------------------

        return _mapper.Map<SensorLogFileDto>(
            entity);
    }
}