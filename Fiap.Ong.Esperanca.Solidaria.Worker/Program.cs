using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Fiap.Ong.Esperanca.Solidaria.Worker.Consumers;

var builder = Host.CreateDefaultBuilder(args)
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
                // Ajuste a configuração conforme seu ambiente RabbitMQ
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                // Automaticamente configura endpoints para consumidores registrados
                cfg.ConfigureEndpoints(context);
            });
        });

        // Inicia o bus com o host
        services.AddMassTransitHostedService();
    });

var host = builder.Build();
await host.RunAsync();
