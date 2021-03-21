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
                await _publish.Publish(new ValueEntered($"{i}.Light.Red"), ctx => ctx.SetRoutingKey("light.red") );
                //await _publish.Publish(new ValueEntered($"{i}.Dark.Red"), ctx => ctx.SetRoutingKey("dark.red"));
                //await _publish.Publish(new ValueEntered($"{i}.Light.Blue"), ctx => ctx.SetRoutingKey("light.blue"));
                //await _publish.Publish(new ValueEntered($"{i}.Light.Blue"), ctx => ctx.SetRoutingKey("light.blue"));
                await _publish.Publish(new ValueEntered($"{i}.Blue with many"), ctx => ctx.TrySetRoutingKey("light.red"),  stoppingToken);
            };
        }
    }
}
