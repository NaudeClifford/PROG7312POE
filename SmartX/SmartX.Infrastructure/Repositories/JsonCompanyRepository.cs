using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories
{
    public class JsonCompanyRepository : ICompanyRepository
    {
        private readonly string _filePath;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public JsonCompanyRepository()
        {
            _filePath = Path.Combine(
                AppContext.BaseDirectory,
                "Data", "Local", "company.json");
        }

        public async Task<Company?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var company = await GetAllAsync(cancellationToken);

            return company.FirstOrDefault(x => x.Id == id);
        }

        public async Task<IReadOnlyList<Company>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_filePath)) return [];

            string json = await File.ReadAllTextAsync(
                _filePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(json)) return [];

            return JsonSerializer.Deserialize<List<Company>>(
                       json,
                       JsonOptions)
                   ?? [];
        }

        public async Task AddAsync(
            Company company,
            CancellationToken cancellationToken = default)
        {
            var companies = (await GetAllAsync(cancellationToken)).ToList();

            companies.Add(company);

            await SaveAsync(
                companies,
                cancellationToken);
        }

        public async Task UpdateAsync(
            Company company,
            CancellationToken cancellationToken = default)
        {
            var companies = (await GetAllAsync(cancellationToken)).ToList();

            var index = companies.FindIndex(
                x => x.Id == company.Id);

            if (index == -1)
                return;

            companies[index] = company;

            await SaveAsync(
                companies,
                cancellationToken);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var companies = (await GetAllAsync(cancellationToken)).ToList();

            var removed = companies.RemoveAll(
                x => x.Id == id);

            if (removed == 0)
                return;

            await SaveAsync(
                companies,
                cancellationToken);
        }

        private async Task SaveAsync(
        IReadOnlyList<Company> companies,
        CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(
                companies,
                JsonOptions);

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }
    }
}
