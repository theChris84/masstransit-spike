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

                //RabbitMQ publish with routing key information
                //await _publish.Publish(new SomeValue($"{i}.Dark.Red"), ctx => ctx.SetRoutingKey("dark.red"));

                //Azure Service Bus with header information
                await _publish.Publish(new SomeValue { Payload = $"{i}.Dark.Red" }, x => x.Headers.Set("RoutingKey", "dark.red"));
                await _publish.Publish(new SomeValue { Payload = $"{i}.Light.Red" }, x => x.Headers.Set("RoutingKey", "light.red"));
                await _publish.Publish(new SomeValue { Payload = $"{i}.Dark.Blue" }, x => x.Headers.Set("RoutingKey", "dark.blue"));

                //await _publish.Publish(new SomeValue { Payload = $"{i}.Light.Blue" });
                //await _publish.Publish(new SomeValue { Payload = $"{i}.Dark.Red" });
                //await _publish.Publish(new SomeValue { Payload = $"{i}.Light.Red" });
                //await _publish.Publish(new SomeValue { Payload = $"{i}.Dark.Blue" });
            };
        }
    }
}
