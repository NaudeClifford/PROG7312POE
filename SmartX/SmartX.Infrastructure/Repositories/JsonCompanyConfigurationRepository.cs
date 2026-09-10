using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories;

public class JsonCompanyConfigurationRepository
    : ICompanyConfigurationRepository
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public JsonCompanyConfigurationRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Local",
            "companyServiceConfiguration.json");
    }

    public async Task<CompanyConfiguration?> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return null;

        var configurations =
            await GetAllAsync(cancellationToken);

        return configurations.FirstOrDefault(
            x => x.CompanyId == companyId);
    }

    public async Task AddAsync(
        CompanyConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var configurations =
            (await GetAllAsync(cancellationToken)).ToList();

        configurations.RemoveAll(
            x => x.CompanyId == configuration.CompanyId);

        configurations.Add(configuration);

        await SaveAsync(
            configurations,
            cancellationToken);
    }

    public async Task UpdateAsync(
        CompanyConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var configurations =
            (await GetAllAsync(cancellationToken)).ToList();

        var index = configurations.FindIndex(
            x => x.CompanyId == configuration.CompanyId);

        if (index == -1)
            return;

        configurations[index] = configuration;

        await SaveAsync(
            configurations,
            cancellationToken);
    }

    private async Task<IReadOnlyList<CompanyConfiguration>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(
            _filePath,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<
                   List<CompanyConfiguration>>(
                       json,
                       JsonOptions)
               ?? [];
    }

    private async Task SaveAsync(
        IReadOnlyList<CompanyConfiguration> configurations,
        CancellationToken cancellationToken)
    {
        var directory =
            Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json =
            JsonSerializer.Serialize(
                configurations,
                JsonOptions);

        await File.WriteAllTextAsync(
            _filePath,
            json,
            cancellationToken);
    }

    public async Task DeleteAsync(
    Guid companyId,
    CancellationToken cancellationToken = default)
    {
        var records = await GetAllAsync(cancellationToken);

        var list =
            records.ToList();

        var configuration = list.FirstOrDefault(x => x.CompanyId == companyId);

        if (configuration is null)
            return;

        list.Remove(configuration);

        var directory =
            Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json =
            JsonSerializer.Serialize(
                list,
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
