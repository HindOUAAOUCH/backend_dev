using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Infrastructure.Mappers;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;
using MongoDB.Driver;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;

public sealed class AddressRepository : IAddressRepository
{
    private readonly IMongoCollection<AddressCorrectionDocument> _collection;

    public AddressRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<AddressCorrectionDocument>("address_corrections");
    }

    public async Task<AddressCorrectionRecord?> FindByNormalizedAddressAsync(string normalizedAddress)
    {
        var doc = await _collection
            .Find(a => a.NormalizedAddress == normalizedAddress)
            .FirstOrDefaultAsync();
        return doc is null ? null : AddressCorrectionDocumentMapper.ToEntity(doc);
    }

    public async Task SaveAsync(AddressCorrectionRecord record)
    {
        var doc = AddressCorrectionDocumentMapper.ToDocument(record);
        await _collection.InsertOneAsync(doc);
    }
}