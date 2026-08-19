using MongoDB.Driver;
using SmartX.Domain.Entities;
using SmartX.Infrastructure.Persistence.Mongo;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartX.Infrastructure.Repositories
{
    public class CompanyRepository
    {
        private readonly IMongoCollection<Company> _collection;

        public CompanyRepository(MongoContext context)
        {
            _collection = context.GetCollection<Company>("Compaies");
        }

        public async Task<Company?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Company>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(_ => true)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            Company company,
            CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(
                company,
                cancellationToken: cancellationToken);
        }

        public async Task UpdateAsync(
            Company company,
            CancellationToken cancellationToken = default)
        {
            await _collection.ReplaceOneAsync(
                x => x.Id == company.Id,
                company,
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
