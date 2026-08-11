using MongoDB.Driver;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Infrastructure.Persistence.Mongo;

namespace SmartX.Infrastructure.Repositories;

public class TelemetryRepository : ITelemetryRepository
{
    private readonly IMongoCollection<Telemetry> _collection;

    public TelemetryRepository(MongoContext context)
    {
        _collection = context.GetCollection<Telemetry>("Telemetry");
    }

    public async Task AddAsync(
        Telemetry telemetry,
        CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(
            telemetry,
            cancellationToken: cancellationToken);
    }

    public async Task<Telemetry?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken) 
    {
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Telemetry>> GetBySensorIdAsync(
        Guid sensorId, CancellationToken cancellationToken)
    {
        return await _collection
            .Find(x =>x.SensorId == sensorId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Telemetry?> GetLatestBySensorIdAsync(
        Guid sensorId, CancellationToken cancellationToken = default) 
    {
        return await _collection
            .Find(x => x.SensorId == sensorId)
            .SortByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Telemetry>> GetBySensorAndDateAsync(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Telemetry>.Filter.And(
            Builders<Telemetry>.Filter.Eq(
                x => x.SensorId,
                sensorId),

            Builders<Telemetry>.Filter.Gte(
                x => x.Timestamp,
                from),

            Builders<Telemetry>.Filter.Lte(
                x => x.Timestamp,
                to)
        );

        return await _collection
            .Find(filter)
            .SortBy(x => x.Timestamp)
            .ToListAsync(cancellationToken);
    }
}