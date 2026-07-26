using Elastic.Apm.Api;
using Fiap.Ong.Esperanca.Solidaria.Contracts.Events;
using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Entities;
using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Repositories;
using MassTransit;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Api.Consumers;

public class DonationReceivedConsumer : IConsumer<DonationReceivedEvent>
{
    private readonly ILogger<DonationReceivedConsumer> _logger;
    private readonly IDonationRepository _donationRepository;

    public DonationReceivedConsumer(ILogger<DonationReceivedConsumer> logger, IDonationRepository donationRepository)
    {
        _logger = logger;
        _donationRepository = donationRepository;
    }

    public async Task Consume(ConsumeContext<DonationReceivedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Received DonationReceivedEvent: CampaignId={CampaignId} DonorId={DonorId} Amount={Amount} Timestamp={Timestamp}",
            msg.CampaignId, msg.DonorId, msg.Amount, msg.Timestamp);

        // Simula processamento 
        await Task.Delay(500);

        // create donation
        var donation = new Donation
        {
            CampaignId = msg.CampaignId,
            CampaignTitle = msg.CampaignTitle,
            DonorId = msg.DonorId,
            Amount = msg.Amount,
            Status = Domain.Enums.DonationStatus.Processed,
            PaymentMethod = Domain.Enums.PaymentMethod.Pix
        };

        await _donationRepository.CreateAsync(donation);
        _logger.LogInformation("Donation created for CampaignId={CampaignId}, Amount={Amount}", msg.CampaignId, msg.Amount);

        var processed = new DonationProcessedEvent {
            CampaignId= msg.CampaignId,
            DonorId= msg.DonorId,
            Amount= msg.Amount,
            Timestamp = msg.Timestamp
        };

        // Publica o evento de doação processada
        await context.Publish(processed);

        _logger.LogInformation("Published DonationProcessedEvent: CampaignId={CampaignId} DonorId={DonorId}", msg.CampaignId, msg.DonorId);
    }
}
