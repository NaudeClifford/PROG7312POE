using MongoDB.Driver;
using SmartX.Domain.Entities;
using SmartX.Infrastructure.Persistence.Mongo;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Infrastructure.Repositories
{
    public class GatewayRepository
    {
        private readonly IMongoCollection<Gateway> _collection;

        public GatewayRepository(MongoContext context)
        {
            _collection = context.GetCollection<Gateway>("Gateways");
        }

        public async Task<Gateway?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Gateway>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(_ => true)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Gateway gateway,
            CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(
                gateway,
                cancellationToken: cancellationToken);
        }

        public async Task UpdateAsync(
            Gateway gateway,
            CancellationToken cancellationToken = default)
        {
            await _collection.ReplaceOneAsync(
                x => x.Id == gateway.Id,
                gateway,
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
}
