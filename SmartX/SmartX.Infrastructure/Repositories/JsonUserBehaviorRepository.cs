
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using System.Text.Json;

namespace SmartX.Infrastructure.Repositories
{
    public class JsonUserBehaviorRepository : IUserBehaviorRepository
    {
        private readonly string _filePath;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public JsonUserBehaviorRepository()
        {
            _filePath = Path.Combine(
                AppContext.BaseDirectory,
                "Data", "Local", "userbehaviors.json");
        }

        public async Task<UserBehavior?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var behaviors = await GetAllAsync(cancellationToken);

            return behaviors.FirstOrDefault(x => x.Id == id);
        }

        public async Task<IReadOnlyList<UserBehavior>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_filePath)) return [];

            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);

            if (string.IsNullOrWhiteSpace(json)) return [];

            return JsonSerializer.Deserialize<List<UserBehavior>>(json, JsonOptions) ?? [];
        }

        public async Task AddAsync(
            UserBehavior userBehavior,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userBehavior);

            var userBehaviors = (await GetAllAsync(cancellationToken)).ToList();

            if (userBehaviors.Any(x => x.Id == userBehavior.Id))
            {
                throw new InvalidOperationException($"UserBehavior with Id {userBehavior.Id} already exists.");
            }

            userBehaviors.Add(userBehavior);

            await SaveAllAsync(userBehaviors, cancellationToken);
        }

        public async Task UpdateAsync(
            UserBehavior userBehavior,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userBehavior);

            var behaviors = (await GetAllAsync(cancellationToken)).ToList();

            var index = behaviors.FindIndex(
                x => x.Id == userBehavior.Id);

            if (index < 0)
                return;

            behaviors[index] = userBehavior;

            await SaveAllAsync(behaviors, cancellationToken);
        }

        public async Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var behaviors = (await GetAllAsync(cancellationToken)).ToList();

            var removed = behaviors.RemoveAll(x => x.Id == id);

            if (removed == 0)
                return;

            await SaveAllAsync(behaviors, cancellationToken);
        }

        public async Task<IReadOnlyList<UserBehavior>> GetSensorByIdAsync(
        Guid sensorId,
        CancellationToken cancellationToken = default)
        {
            if (sensorId == Guid.Empty)
                return [];

            var behaviors = await GetAllAsync(cancellationToken);

            return behaviors
                .Where(x => x.SensorId == sensorId)
                .OrderByDescending(x => x.Timestamp)
                .ToList();
        }

        public async Task<IReadOnlyList<UserBehavior>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
                return [];

            var behaviors = await GetAllAsync(cancellationToken);

            return behaviors
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Timestamp)
                .ToList();
        }

        private async Task SaveAllAsync(
            IReadOnlyList<UserBehavior> behaviors,
            CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(
                behaviors,
                JsonOptions);

            await File.WriteAllTextAsync(
                _filePath,
                json,
                cancellationToken);
        }
    }
}
