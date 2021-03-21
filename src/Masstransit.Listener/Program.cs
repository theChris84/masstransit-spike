using MassTransit;
using MassTransit.SharedTypes;
using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Masstransit.Listener
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new Program().RunAsync();
        }

        private async Task RunAsync()
        {
            var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
           {
               cfg.Message<ValueEntered>(x => x.SetEntityName("value.entered"));


               cfg.ReceiveEndpoint("blue.client.valueEntered", endpoint =>
               {
                   endpoint.ConfigureConsumeTopology = false;

                   endpoint.Bind<ValueEntered>(exCfg => {
                       exCfg.RoutingKey = "#.blue";
                       exCfg.ExchangeType = ExchangeType.Topic;
                   });

                   endpoint.Handler<ValueEntered>(ctx => {
                       Console.WriteLine($"Value: {ctx.Message.Value}");
                       return Task.CompletedTask;
                   });
               });
           });

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await bus.StartAsync(cts.Token);

            try
            {
                Console.WriteLine("Press enter to exit");
                await Task.Run(Console.ReadLine, cts.Token);

            }
            finally
            {

                await bus.StopAsync(cts.Token);
            }
        }

        private class EventConsumer : IConsumer<ValueEntered>
        {
            public Task Consume(ConsumeContext<ValueEntered> context)
            {
                Console.WriteLine($"Value: {context.Message.Value}");
                return Task.CompletedTask;
            }
        }
    }
}
