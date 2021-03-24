using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.SharedTypes;
using Microsoft.Extensions.Hosting;

namespace Masstransit.Publisher
{
    internal class SimplePublisherService : BackgroundService
    {
        private readonly IBus _bus;
        private readonly IPublishEndpoint _publish;

        public SimplePublisherService(IBus bus, IPublishEndpoint publish)
        {
            _bus = bus;
            _publish = publish;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(100);

            foreach (var i in Enumerable.Range(1, 11))
            {

                
                Console.Write($"Messages Published count: {i}");
                //await _publish.Publish(new ValueEntered($"{i}.Light.Red"), ctx => ctx.SetRoutingKey("light.red"));
                //await _publish.Publish(new ValueEntered($"{i}.Dark.Red"), ctx => ctx.SetRoutingKey("dark.red"));
                //await _publish.Publish(new ValueEntered($"{i}.Light.Blue"), ctx => ctx.SetRoutingKey("light.blue"));
                //await _publish.Publish(new ValueEntered($"{i}.Dark.Blue"), ctx => ctx.SetRoutingKey("dark.blue"));

                await _publish.Publish(new ValueEntered($"{i}.Light.Red"), x => x.Headers.Set("ValueEntered", "light.red"));
                await _publish.Publish(new ValueEntered($"{i}.Dark.Red"), x => x.Headers.Set("ValueEntered", "dark.red"));
                await _publish.Publish(new ValueEntered($"{i}.Light.Blue"), x => x.Headers.Set("ValueEntered", "light.blue"));
                await _publish.Publish(new ValueEntered($"{i}.Dark.Blue"), x => x.Headers.Set("ValueEntered", "dark.blue"));
            };
        }
    }
}
