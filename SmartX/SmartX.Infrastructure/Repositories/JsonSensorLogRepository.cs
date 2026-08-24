using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories;

public class JsonSensorLogFileRepository : ISensorLogFileRepository
{
    private readonly string _filePath;

    public JsonSensorLogFileRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Local",
            "sensor-log-files.json");
    }

    public async Task<SensorLogFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetAllAsync(cancellationToken);

        return logs.FirstOrDefault(x => x.Id == id);
    }

    public async Task<IReadOnlyList<SensorLogFile>> GetBySensorIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetAllAsync(cancellationToken);

        return logs
            .Where(x => x.SensorId == sensorId)
            .OrderByDescending(x => x.UploadedAt)
            .ToList();
    }

    public async Task<IReadOnlyList<SensorLogFile>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(
            _filePath,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
            return [];

        var logs =
            JsonSerializer.Deserialize<List<SensorLogFile>>(json);

        return logs ?? [];
    }

    public async Task AddAsync(
        SensorLogFile logFile,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetAllAsync(cancellationToken);

        var list = logs.ToList();

        list.Add(logFile);

        await SaveAsync(list, cancellationToken);
    }

    public async Task UpdateAsync(
        SensorLogFile logFile,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetAllAsync(cancellationToken);

        var list = logs.ToList();

        var existing = list.FirstOrDefault(
            x => x.Id == logFile.Id);

        if (existing is null)
            return;

        var index = list.IndexOf(existing);

        list[index] = logFile;

        await SaveAsync(list, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetAllAsync(cancellationToken);

        var list = logs.ToList();

        var existing = list.FirstOrDefault(
            x => x.Id == id);

        if (existing is null)
            return;

        list.Remove(existing);

        await SaveAsync(list, cancellationToken);
    }

    private async Task SaveAsync(
        List<SensorLogFile> logs,
        CancellationToken cancellationToken)
    {
        var directory =
            Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(
            logs,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(
            _filePath,
            json,
            cancellationToken);
    }
}