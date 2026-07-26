using Fiap.Ong.Esperanca.Solidaria.Worker.Application.Dto.Donations;
using Fiap.Ong.Esperanca.Solidaria.Worker.Application.Interfaces;
using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Application.Services;

public class DonationService : IDonationService
{
    private readonly IDonationRepository _donationRepository;
    private readonly ILogger<DonationService> _logger;

    public DonationService(IDonationRepository donationRepository, ILogger<DonationService> logger)
    {
        _donationRepository = donationRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<DonationDto>> GetDonationsByDonorAsync(string donorId)
    {
        var donations = await _donationRepository.GetByDonorIdAsync(donorId);

        return donations.Select(d => new DonationDto
        {
            Id = d.Id,
            CampaignId = d.CampaignId,
            CampaignTitle = d.CampaignTitle, 
            Amount = d.Amount,
            CreatedAt = d.CreatedAt,
            Status = d.Status,
            PaymentMethod = d.PaymentMethod
        });
    }
}
