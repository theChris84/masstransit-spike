using MassTransit;
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

namespace Masstransit.Publisher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
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

        private IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            return Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(cfg =>
                {
                    cfg.SetBasePath(Directory.GetCurrentDirectory());
                    cfg.AddCommandLine(args);
                })
                .ConfigureServices((_, services) =>
                {
                    ConfigureMassTransit(services);
                    services.AddHostedService<HostedService>();
                    services.AddHostedService<SimplePublisherService>();
                })
                .UseSerilog();

        }

        private void ConfigureMassTransit(IServiceCollection services)
        {
            //Azure Service Bus
            //var azureServiceBus = Bus.Factory.CreateUsingAzureServiceBus(config =>
            //{
            //    config.Host("Endpoint=sb://masstranis-spike.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=cu1mzcm97sy6RS8sDW3d5q5vcRHGKwLvAnaywuGNF0E=",
            //        hostConfig =>
            //        {
            //            hostConfig.TransportType = TransportType.AmqpWebSockets;
            //            hostConfig.TokenProvider =
            //                TokenProvider.CreateSharedAccessSignatureTokenProvider("RootManageSharedAccessKey", 
            //                    "cu1mzcm97sy6RS8sDW3d5q5vcRHGKwLvAnaywuGNF0E=");
            //        });
            //    config.Message<ValueEntered>(x => x.SetEntityName("value.entered"));
            //});



            services.AddMassTransit(config =>
            {
                //config.AddBus(provider => azureServiceBus);
                config.UsingAzureServiceBus((_, azureConfig) =>
                {
                    azureConfig.Host("Endpoint=sb://masstranis-spike.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=cu1mzcm97sy6RS8sDW3d5q5vcRHGKwLvAnaywuGNF0E=",
                        hostConfig =>
                        {
                            hostConfig.TransportType = TransportType.AmqpWebSockets;
                            hostConfig.TokenProvider =
                                TokenProvider.CreateSharedAccessSignatureTokenProvider("RootManageSharedAccessKey",
                                    "cu1mzcm97sy6RS8sDW3d5q5vcRHGKwLvAnaywuGNF0E=");
                        });
                    azureConfig.Message<ValueEntered>(x => x.SetEntityName("value.entered"));
                    azureConfig.Publish<ValueEntered>(x => { x.EnablePartitioning = true;});
                });


                //rabbitMQ
                /*config.UsingRabbitMq((context, rabbitConfig) =>
                {

                    var hostSettings = new RabbitMqHostConfigurator("localhost", "/");
                    rabbitConfig.Host(hostSettings.Settings);

                    //cfg.MessageTopology.SetEntityNameFormatter(new CustomEntityNameFormatter());
                    rabbitConfig.Message<ValueEntered>(x => x.SetEntityName("value.entered"));
                    rabbitConfig.Publish<ValueEntered>(x => { x.ExchangeType = ExchangeType.Topic; });
                    //cfg.ConfigureEndpoints(context);
                });*/
            });

            //services.AddSingleton<IPublishEndpoint>(azureServiceBus);
            //services.AddSingleton<ISendEndpointProvider>(azureServiceBus);
            //services.AddSingleton<IBus>(azureServiceBus);
        }
    }
}
