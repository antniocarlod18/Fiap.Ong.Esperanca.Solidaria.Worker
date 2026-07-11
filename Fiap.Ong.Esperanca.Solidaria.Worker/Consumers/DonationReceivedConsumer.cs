using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Fiap.Ong.Esperanca.Solidaria.Worker.Contracts;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Consumers;

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
        _logger.LogInformation("Recebido DonationReceivedEvent: CampaignId={CampaignId} DonorId={DonorId} Amount={Amount} Timestamp={Timestamp}",
            msg.CampaignId, msg.DonorId, msg.Amount, msg.Timestamp);

        // Simula processamento (validações, gravação, integrações etc.)
        await Task.Delay(500);

        var processed = new DonationProcessedEvent(
            CampaignId: msg.CampaignId,
            DonorId: msg.DonorId,
            Amount: msg.Amount,
            ReceivedTimestamp: msg.Timestamp,
            ProcessedTimestamp: DateTime.UtcNow,
            Success: true
        );

        // Publica o evento de doação processada
        await context.Publish(processed);

        _logger.LogInformation("Publicado DonationProcessedEvent: CampaignId={CampaignId} DonorId={DonorId}", msg.CampaignId, msg.DonorId);
    }
}
