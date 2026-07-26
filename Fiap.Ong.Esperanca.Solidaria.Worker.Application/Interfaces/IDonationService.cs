using Fiap.Ong.Esperanca.Solidaria.Worker.Application.Dto.Donations;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Application.Interfaces;

public interface IDonationService
{
    Task<IEnumerable<DonationDto>> GetDonationsByDonorAsync(string donorId);
}
