using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories
{
    public class JsonCompanyRepository : ICompanyRepository
    {
        private readonly string _filePath;

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

            var company = JsonSerializer.Deserialize<List<Company>>(json);

            return company ?? [];
        }

        public async Task AddAsync(
            Company company,
            CancellationToken cancellationToken = default)
        {
            var companys = await GetAllAsync(cancellationToken);

            var companyList = companys.ToList();

            companyList.Add(company);

            string json = JsonSerializer.Serialize(companyList,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }

        public async Task UpdateAsync(
            Company company,
            CancellationToken cancellationToken = default)
        {
            var companys = await GetAllAsync(cancellationToken);

            var companyList = companys.ToList();

            var existingCompany = companyList.FirstOrDefault(
                x => x.Id == company.Id);

            if (existingCompany is null) return;

            var index = companyList.IndexOf(existingCompany);

            companyList[index] = company;

            string json = JsonSerializer.Serialize(companyList,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var companys = await GetAllAsync(cancellationToken);

            var companysList = companys.ToList();

            var company = companysList.FirstOrDefault(x => x.Id == id);

            if (company is null) return;

            companysList.Remove(company);

            string json = JsonSerializer.Serialize(companysList,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }

    }
}
