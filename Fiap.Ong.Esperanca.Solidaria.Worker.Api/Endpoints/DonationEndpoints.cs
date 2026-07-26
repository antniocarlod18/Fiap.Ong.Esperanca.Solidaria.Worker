using Fiap.Ong.Esperanca.Solidaria.Worker.Application.Dto.Donations;
using Fiap.Ong.Esperanca.Solidaria.Worker.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Fiap.Ong.Esperanca.Solidaria.Api.Endpoints;

public static class DonationEndpoints
{
    public static void MapDonationEndpoints(this WebApplication app)
    {
        // Get donations for authenticated donor
        app.MapGet("/donations", async (IDonationService donationService, ClaimsPrincipal user) =>
        {
            var donorId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
            var donations = await donationService.GetDonationsByDonorAsync(donorId);
            return Results.Ok(donations);
        }).RequireAuthorization(new AuthorizeAttribute { Roles = "Donor,ManagerOng" });
    }
}
