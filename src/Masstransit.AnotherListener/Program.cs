using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.SharedTypes;
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

            //Masstransit connect to RabbitMQ
            //var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            //{
            //    cfg.Host("localhost", "/");
            //    cfg.Message<SomeValue>(x => x.SetEntityName("value.entered"));
            //    cfg.ReceiveEndpoint("red.client.valueEntered", endpoint =>
            //    {
            //        //cfg.PrefetchCount = 1;
            //        endpoint.ConfigureConsumeTopology = false;
            //        endpoint.AutoDelete = true;

            //        //RabbitMQ attach consumer based on topic information
            //        endpoint.Bind<SomeValue>(x =>
            //        {
            //            x.RoutingKey = "*.red";
            //            x.ExchangeType = ExchangeType.Topic;
            //        });

            //        //RabbitMQ attach consumer based on header information
            //       //endpoint.Bind<SomeValue>(x =>
            //       //{
            //       //    x.SetBindingArgument("SomeValue", "dark.red");
            //       //    x.ExchangeType = ExchangeType.Headers;
            //       //});

            //        endpoint.Handler<SomeValue>(ctx =>
            //        {
            //            Console.WriteLine($"Payload: {ctx.Message.Payload}");
            //            return Task.CompletedTask;
            //        });

            //        endpoint.Consumer<RoutingEventConsumer>();
            //    });
            //});


            //Masstrasit connect to azure service bus

            var bus = Bus.Factory.CreateUsingAzureServiceBus(config =>
            {
                config.Host(
                    "Endpoint=sb://masstranis-spike.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=cu1mzcm97sy6RS8sDW3d5q5vcRHGKwLvAnaywuGNF0E=",
                    hostConfig =>
                    {
                        hostConfig.OperationTimeout = TimeSpan.FromSeconds(5);
                        hostConfig.TransportType = TransportType.AmqpWebSockets;
                    });

                config.Message<SomeValue>(x => x.SetEntityName("contracts-somevalue"));

                config.SubscriptionEndpoint<SomeValue>("only-red", endpoint =>
                {

                    var endpointFilter = new SqlFilter("RoutingKey LIKE '%.red'");
                    //endpoint.Filter = endpointFilter;
                    endpoint.Rule = new RuleDescription("only-dark-red-rule", endpointFilter);
                    endpoint.Handler<SomeValue>(ctx =>
                    {
                        Console.WriteLine($"Payload: {ctx.Message.Payload}");
                        return Task.CompletedTask;
                    });
                } );
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

internal class RoutingEventConsumer : IConsumer<SomeValue>
{
    public Task Consume(ConsumeContext<SomeValue> context)
    {
        Console.WriteLine($"Payload: {context.Message.Payload}");
        return Task.CompletedTask;
    }
}