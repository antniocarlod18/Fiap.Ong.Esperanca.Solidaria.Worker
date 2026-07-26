using Elastic.Apm.SerilogEnricher;
using Elastic.Channels;
using Elastic.CommonSchema.Serilog;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Fiap.Ong.Esperanca.Solidaria.Api.Endpoints;
using Fiap.Ong.Esperanca.Solidaria.Worker.Api.Consumers;
using Fiap.Ong.Esperanca.Solidaria.Worker.Api.Filters;
using Fiap.Ong.Esperanca.Solidaria.Worker.Api.HealthChecks;
using Fiap.Ong.Esperanca.Solidaria.Worker.Application.Interfaces;
using Fiap.Ong.Esperanca.Solidaria.Worker.Application.Services;
using Fiap.Ong.Esperanca.Solidaria.Worker.Application.Settings;
using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Entities;
using Fiap.Ong.Esperanca.Solidaria.Worker.Domain.Repositories;
using Fiap.Ong.Esperanca.Solidaria.Worker.Infra.Data.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using System.Text;
using System.Text.Json;

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

// Bind settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddSingleton(jwtSettings);

var mongoSettings = builder.Configuration.GetSection("MongoSettings").Get<MongoSettings>();
var mongoClient = new MongoClient(mongoSettings.ConnectionString);
var mongoDb = mongoClient.GetDatabase(mongoSettings.DatabaseName);


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddAuthorization();

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

builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddSingleton<IDonationRepository>(sp => new MongoDonationRepository(mongoDb.GetCollection<Donation>("donations")));

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

// Global exception handling middleware
app.UseMiddleware<Fiap.Ong.Esperanca.Solidaria.Api.Middlewares.ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.MapDonationEndpoints();

app.UseAuthentication();
app.UseAuthorization();

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