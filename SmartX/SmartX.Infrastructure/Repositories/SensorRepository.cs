using MongoDB.Driver;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Infrastructure.Persistence.Mongo;

namespace SmartX.Infrastructure.Repositories;

public class SensorRepository : ISensorRepository
{
    private readonly IMongoCollection<Sensor> _collection;

    public SensorRepository(MongoContext context)
    {
        _collection = context.GetCollection<Sensor>("Sensors");
    }

    public async Task<Sensor?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sensor>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(_ => true)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(
            sensor,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(
        Sensor sensor,
        CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(
            x => x.Id == sensor.Id,
            sensor,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(
            x => x.Id == id,
            cancellationToken);
    }
}