using System;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Contracts;

// Evento recebido quando uma doação é criada
public record DonationReceivedEvent(
    string CampaignId,
    string DonorId,
    decimal Amount,
    DateTime Timestamp
);

// Evento publicado após o processamento da doação
public record DonationProcessedEvent(
    string CampaignId,
    string DonorId,
    decimal Amount,
    DateTime ReceivedTimestamp,
    DateTime ProcessedTimestamp,
    bool Success
);