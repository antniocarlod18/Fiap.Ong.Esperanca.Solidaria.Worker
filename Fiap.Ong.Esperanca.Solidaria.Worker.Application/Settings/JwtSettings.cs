namespace Fiap.Ong.Esperanca.Solidaria.Worker.Application.Settings;

public class JwtSettings
{
    public string Secret { get; set; } = "replace_this_long_secret_for_prod";
    public string Issuer { get; set; } = "Fiap.Ong";
    public string Audience { get; set; } = "Fiap.Ong.Clients";
    public int ExpiresMinutes { get; set; } = 60;
}
