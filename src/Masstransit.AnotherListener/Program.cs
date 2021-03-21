using MassTransit;
using MassTransit.SharedTypes;
using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit.ConsumeConfigurators;
using Microsoft.Azure.ServiceBus;

namespace Masstransit.AnotherListener
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await new Program().RunAsync();
        }

        private async Task RunAsync()
        {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            //var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            //{
            //    cfg.Host("localhost", "/");

            //    cfg.Message<ValueEntered>(x => x.SetEntityName("value.entered"));

            //    cfg.PrefetchCount = 1;
            //    cfg.ReceiveEndpoint("red.client.valueEntered", endpoint =>
            //    {
            //        cfg.PrefetchCount = 1;

            //        endpoint.ConfigureConsumeTopology = false;
            //        endpoint.AutoDelete = true;

            //        endpoint.Bind<ValueEntered>(exCfg =>
            //        {
            //            exCfg.RoutingKey = "*.red";
            //            exCfg.ExchangeType = ExchangeType.Topic;
            //        });

            //        endpoint.Handler<ValueEntered>(ctx =>
            //        {
            //            Console.WriteLine($"Value: {ctx.Message.Value}");
            //            return Task.CompletedTask;
            //        });

            //        endpoint.Consumer<RoutingEventConsumer>();
            //    });
            //});

            var bus = Bus.Factory.CreateUsingAzureServiceBus(config =>
                {
                    config.Message<ValueEntered>(m => { m.SetEntityName("value.entered"); });
                    config.Host("Endpoint=sb://masstranis-spike.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=cu1mzcm97sy6RS8sDW3d5q5vcRHGKwLvAnaywuGNF0E=",
                        hostConfig =>
                        {
                            hostConfig.OperationTimeout = TimeSpan.FromSeconds(5);
                            hostConfig.TransportType = TransportType.AmqpWebSockets;
                        });

                    config.SubscriptionEndpoint<ValueEntered>("value.entered", endpoint =>
                    {
                        endpoint.Handler<ValueEntered>(ctx =>
                        {
                            Console.WriteLine($"Value: {ctx.Message.Value}");
                            return Task.CompletedTask;
                        });
                    });
                });

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
    }
    
}

internal class RoutingEventConsumer : IConsumer<ValueEntered>
{
    public Task Consume(ConsumeContext<ValueEntered> context)
    {
        Console.WriteLine($"Value: {context.Message.Value}");
        return Task.CompletedTask;
    }
}