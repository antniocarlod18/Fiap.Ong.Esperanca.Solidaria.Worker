using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Entities;
using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Repositories;
using MongoDB.Driver;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Infra.Data.Repositories;

public class MongoDonationRepository : IDonationRepository
{
    private readonly IMongoCollection<Donation> _collection;

    public MongoDonationRepository(IMongoCollection<Donation> collection)
    {
        _collection = collection;
    }

    public async Task CreateAsync(Donation donation)
    {
        await _collection.InsertOneAsync(donation);
    }

    public async Task<IEnumerable<Donation>> GetByDonorIdAsync(string donorId)
    {
        var filter = Builders<Donation>.Filter.Eq(d => d.DonorId, donorId);
        return await _collection.Find(filter).ToListAsync();
    }
}
