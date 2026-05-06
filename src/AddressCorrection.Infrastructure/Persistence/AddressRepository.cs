using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;
using MongoDB.Driver;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;

public sealed class AddressRepository : IAddressRepository
{
    private readonly IMongoCollection<AddressCorrectionRecord> _collection;

    public AddressRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<AddressCorrectionRecord>("address_corrections");
    }

    public async Task<AddressCorrectionRecord?> FindByNormalizedAddressAsync(string normalizedAddress)
    {
        return await _collection
            .Find(a => a.NormalizedAddress == normalizedAddress)
            .FirstOrDefaultAsync();
    }

    public async Task SaveAsync(AddressCorrectionRecord record)
    {
        await _collection.InsertOneAsync(record);
    }
}