using System.Text.Json;
using System.Net;
using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fiap.Ong.Esperanca.Solidaria.Worker.Consumers;
using Fiap.Ong.Esperanca.Solidaria.Worker.HealthChecks;

var builder = WebApplication
    .CreateBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<DonationReceivedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddMassTransitHostedService();

        services.AddHealthChecks()
            .AddCheck<MassTransitHealthCheck>("masstransit", HealthStatus.Unhealthy, tags: new[] { "live", "ready" });
    });

var host = builder.Build();

static async Task WriteHealthResponse(HttpListenerResponse response, HealthReport report)
{
    response.ContentType = "application/json; charset=utf-8";

    var payload = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.ToString(),
        entries = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            duration = entry.Value.Duration.ToString(),
            error = entry.Value.Exception?.Message
        })
    };

    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    var buffer = System.Text.Encoding.UTF8.GetBytes(json);
    response.ContentLength64 = buffer.Length;
    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    response.OutputStream.Close();
}

_ = Task.Run(async () =>
{
    await host.StartAsync();

    var listener = new HttpListener();
    listener.Prefixes.Add("http://localhost:9000/");
    listener.Start();

    var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();
    var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

    while (!lifetime.ApplicationStopping.IsCancellationRequested)
    {
        try
        {
            var context = listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            string? path = request.Url?.AbsolutePath;

            if (path == "/health")
            {
                var report = await healthCheckService.CheckHealthAsync();
                response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
                await WriteHealthResponse(response, report);
            }
            else if (path == "/health/live")
            {
                var report = await healthCheckService.CheckHealthAsync(predicate: check => check.Tags.Contains("live"));
                response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
                await WriteHealthResponse(response, report);
            }
            else if (path == "/health/ready")
            {
                var report = await healthCheckService.CheckHealthAsync(predicate: check => check.Tags.Contains("ready"));
                response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
                await WriteHealthResponse(response, report);
            }
            else
            {
                response.StatusCode = 404;
                response.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health check endpoint error: {ex.Message}");
        }
    }

    listener.Stop();
});

await host.RunAsync();
