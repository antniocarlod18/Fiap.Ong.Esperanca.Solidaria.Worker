using Fiap.Ong.Esperanca.Solidaria.Worker.Api.Consumers;
using Fiap.Ong.Esperanca.Solidaria.Worker.Api.Filters;
using Fiap.Ong.Esperanca.Solidaria.Worker.Api.HealthChecks;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json;
using Elastic.Apm.SerilogEnricher;
using Elastic.Channels;
using Elastic.CommonSchema.Serilog;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DonationReceivedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UsePublishFilter(typeof(TracingPublishFilter<>), context);

        cfg.UseConsumeFilter(typeof(TracingConsumeFilter<>), context);

        var rabbit = builder.Configuration.GetSection("RabbitMq");

        cfg.Host(
            rabbit["Host"],
            h =>
            {
                h.Username(rabbit["Username"]);
                h.Password(rabbit["Password"]);
            });

        cfg.ConfigureEndpoints(context);
    });
});

// Serilog configuration
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithCorrelationId()
        .Enrich.WithMachineName()
        .Enrich.WithElasticApmCorrelationInfo()
        .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
        .WriteTo.Console()
        .WriteTo.Elasticsearch([new Uri(builder.Configuration["ElasticSearch:Uri"])], opts =>
        {
            opts.DataStream = new DataStreamName("logs", builder.Configuration["ElasticSearch:IndexName"], context.HostingEnvironment.EnvironmentName);
            opts.BootstrapMethod = BootstrapMethod.Failure;
            opts.TextFormatting = new EcsTextFormatterConfiguration<LogEventEcsDocument>();
            opts.ConfigureChannel = channelOpts =>
            {
                channelOpts.BufferOptions = new BufferOptions();
            };
        }, transport =>
        {
            transport.Authentication(new ApiKey(builder.Configuration["ElasticSearch:ApiKey"]));
        });
});

builder.Services.AddAllElasticApm();

// Health Checks
builder.Services
    .AddHealthChecks()

    // Processo vivo
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])

    // Rabbit / MassTransit
    .AddCheck<MassTransitHealthCheck>(
        "masstransit",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

await app.RunAsync();

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json; charset=utf-8";

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

    return context.Response.WriteAsync(
        JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
}