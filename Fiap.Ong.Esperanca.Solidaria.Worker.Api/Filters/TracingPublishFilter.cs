using Elastic.Apm;
using MassTransit;

namespace Fiap.Ong.Esperanca.Solidaria.Worker.Api.Filters;

public class TracingPublishFilter<T> : IFilter<PublishContext<T>> where T : class
{
    public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var transaction = Agent.Tracer.CurrentTransaction;

        if (transaction != null)
        {
            var traceparent = transaction
                .OutgoingDistributedTracingData
                .SerializeToString();

            context.Headers.Set("traceparent", traceparent);
        }

        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("TracingPublishFilter");
    }
}
