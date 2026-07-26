namespace Fiap.Ong.Esperanca.Solidaria.Contracts.Events;

public record DonationProcessedEvent
{
    public string CampaignId { get; init; } = null!;
    public string DonorId { get; init; } = null!;
    public decimal Amount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
