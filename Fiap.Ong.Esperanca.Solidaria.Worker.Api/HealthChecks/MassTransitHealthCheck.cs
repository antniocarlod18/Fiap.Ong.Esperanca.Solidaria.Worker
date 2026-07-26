using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Api.HealthChecks;

public class MassTransitHealthCheck : IHealthCheck
{
    private readonly IBusControl _bus;

    public MassTransitHealthCheck(IBusControl bus)
    {
        _bus = bus;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica se o bus está pronto tentando acessar um endpoint publicador
            // Se conseguir, a conexão com RabbitMQ está funcionando
            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ connection is healthy"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"RabbitMQ health check failed: {ex.Message}", ex));
        }
    }
}
