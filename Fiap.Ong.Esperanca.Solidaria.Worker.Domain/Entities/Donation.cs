using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Enums;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Entities;

public class Donation
{
    public string? Id { get; set; }
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignTitle { get; set; } = string.Empty;
    public string DonorId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DonationStatus Status { get; set; } = DonationStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;
}
