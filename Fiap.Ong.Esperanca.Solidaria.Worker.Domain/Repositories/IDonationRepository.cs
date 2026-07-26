using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Entities;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Repositories;

public interface IDonationRepository
{
    Task CreateAsync(Donation donation);
    Task<IEnumerable<Donation>> GetByDonorIdAsync(string donorId);
}
