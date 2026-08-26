using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories;

public class JsonUserRepository : IUserRepository
{
    private readonly string _filePath;

    public JsonUserRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "Data",
            "Local",
            "users.json");
    }

    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var users = await GetAllAsync(cancellationToken);

        return users.FirstOrDefault(x => x.Id == id);
    }

    public async Task<User?> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUid))
            return null;

        var users =
            await GetAllAsync(cancellationToken);

        return users.FirstOrDefault(
            x => string.Equals(
                x.FirebaseUid,
                firebaseUid,
                StringComparison.Ordinal));
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var users = await GetAllAsync(cancellationToken);

        return users.FirstOrDefault(
            x => x.Email.Equals(
                email,
                StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(
            _filePath,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
            return [];

        var users = JsonSerializer.Deserialize<List<User>>(
            json);

        return users ?? [];
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var users = await GetAllAsync(cancellationToken);

        var userList = users.ToList();

        userList.Add(user);

        await SaveAsync(
            userList,
            cancellationToken);
    }

    public async Task UpdateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var users = await GetAllAsync(cancellationToken);

        var userList = users.ToList();

        var index = userList.FindIndex(
            x => x.Id == user.Id);

        if (index == -1)
            return;

        userList[index] = user;

        await SaveAsync(
            userList,
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var users = await GetAllAsync(cancellationToken);

        var userList = users.ToList();

        var user = userList.FirstOrDefault(
            x => x.Id == id);

        if (user is null)
            return;

        userList.Remove(user);

        await SaveAsync(
            userList,
            cancellationToken);
    }

    private async Task SaveAsync(
        List<User> users,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(
            users,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(
            _filePath,
            json,
            cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByCompanyIdAsync(
    Guid companyId,
    CancellationToken cancellationToken = default)
    {
        var users =
            await GetAllAsync(cancellationToken);

        return users
            .Where(x => x.CompanyId == companyId)
            .ToList();
    }
}