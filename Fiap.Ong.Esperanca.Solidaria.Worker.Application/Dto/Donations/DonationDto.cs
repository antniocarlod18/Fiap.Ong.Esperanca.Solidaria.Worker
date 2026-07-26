using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Enums;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Application.Dto.Donations;

public class DonationDto
{
    public string? Id { get; set; }
    public string CampaignId { get; set; } = string.Empty;
    public string CampaignTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DonationStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}
