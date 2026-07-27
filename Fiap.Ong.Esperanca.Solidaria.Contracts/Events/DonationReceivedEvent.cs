using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Enums;

namespace Fiap.Ong.Esperanca.Solidaria.Contracts.Events;

public record DonationReceivedEvent
{
    public string CampaignId { get; init; } = null!;
    public string CampaignTitle { get; init; } = null!;
    public string DonorId { get; init; } = null!;
    public decimal Amount { get; init; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
