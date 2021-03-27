using MassTransit;
using MassTransit.Azure.ServiceBus.Core.Configurators;
using MassTransit.SharedTypes;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Masstransit.AnotherListener
{
    internal class Program
    {
        public IConfigurationRoot Configuration { get; set; }
        private IHostEnvironment Environment { get; set; }

        private static async Task Main(string[] args)
        {
            try
            {

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("System", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .CreateLogger();

                await new Program()
                    .CreateHostBuilder(args)
                    .Build()
                    .RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private IHostBuilder CreateHostBuilder(string[] args) =>            
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hosting, config) =>
                {
                    config.Sources.Clear();
                    Environment = hosting.HostingEnvironment;
                    config
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{Environment}.json", true, true);
                    Configuration = config.Build();
                })
                .ConfigureHostConfiguration(cfg =>
                {
                    cfg.SetBasePath(Directory.GetCurrentDirectory());
                    cfg.AddCommandLine(args);
                })
                .ConfigureServices((_, services) =>
                {
                    ConfigureMassTransit(services, Configuration);
                })
                .UseSerilog();

        private void ConfigureMassTransit(IServiceCollection services, IConfigurationRoot configurationRoot)
        {

            //Masstransit connect to RabbitMQ
            //var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            //{
            //    cfg.Host("localhost", "/");
            //    cfg.Message<SomeValue>(x => x.SetEntityName($"contracts-{nameof(SomeValue)}"));
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

            //        endpoint.Consumer<OnlyRedMessageHandler>();
            //    });
            //});


            //Masstrasit connect to azure service bus


            var azureConsumerConfiguration = Bus.Factory.CreateUsingAzureServiceBus(config =>
            {
                var azureHostSetting = new HostSettings
                {
                    ServiceUri = new Uri(configurationRoot["AzureServiceBus:Connection"]),
                    OperationTimeout = TimeSpan.FromSeconds(5),
                    TransportType = TransportType.Amqp,
                    TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider("RootManageSharedAccessKey",
                        configurationRoot["AzureServiceBus:SharedKey"])
                };
                config.Host(azureHostSetting);
                config.Message<SomeValue>(x => x.SetEntityName($"contracts-{nameof(SomeValue)}"));

                config.SubscriptionEndpoint<SomeValue>("only-red", endpoint =>
                {
                    var endpointFilter = new SqlFilter("RoutingKey LIKE '%.red'");
                    endpoint.Rule = new RuleDescription("only-dark-red-rule", endpointFilter);

                    //endpoint.Handler<SomeValue>(ctx =>
                    //{
                    //    Console.WriteLine($"Payload: {ctx.Message.Payload}");
                    //    return Task.CompletedTask;
                    //});
                    endpoint.Consumer<OnlyRedMessageHandler>();
                });
            });

            services.AddMassTransit(opt => opt.AddBus(_ => azureConsumerConfiguration));
        }
    }
}

internal class OnlyRedMessageHandler : IConsumer<SomeValue>
{
    public Task Consume(ConsumeContext<SomeValue> context)
    {
        Console.WriteLine($"Payload: {context.Message.Payload}");
        return Task.CompletedTask;
    }
}