using MassTransit;
using Fiap.Ong.Esperanca.Solidaria.Contracts.Events;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Api.Consumers;

public class DonationReceivedConsumer : IConsumer<DonationReceivedEvent>
{
    private readonly ILogger<DonationReceivedConsumer> _logger;

    public DonationReceivedConsumer(ILogger<DonationReceivedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DonationReceivedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Received DonationReceivedEvent: CampaignId={CampaignId} DonorId={DonorId} Amount={Amount} Timestamp={Timestamp}",
            msg.CampaignId, msg.DonorId, msg.Amount, msg.Timestamp);

        // Simula processamento 
        await Task.Delay(500);

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
