using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories;

public class JsonAuditLogRepository : IAuditLogRepository
{
    private readonly string _filePath;

    public JsonAuditLogRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Local",
            "auditlogs.json");
    }

    public async Task<IReadOnlyList<AuditLog>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(
            _filePath,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<AuditLog>>(json) ?? [];
    }

    public async Task AddAsync(
        AuditLog log,
        CancellationToken cancellationToken = default)
    {
        var logs = (await GetAllAsync(
            cancellationToken)).ToList();

        logs.Add(log);

        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

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