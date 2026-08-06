using MongoDB.Driver;
using SmartX.Domain.Entities;
using SmartX.Domain.Interfaces;
using SmartX.Infrastructure.Persistence.Mongo;

namespace SmartX.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _collection;

    public UserRepository(MongoContext context)
    {
        _collection = context.GetCollection<User>("Users");
    }

    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(user => user.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(user => user.FirebaseUid == firebaseUid)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(user => user.Email == email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(
            user,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(
            existing => existing.Id == user.Id,
            user,
            cancellationToken: cancellationToken);
    }
}