using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core.Configurators;
using MassTransit.RabbitMqTransport.Configurators;
using MassTransit.SharedTypes;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Serilog;
using Serilog.Events;

namespace Masstransit.Publisher
{
    internal class Program
    {
        private IHostEnvironment Environment { get;  set; }
        private IConfigurationRoot Configuration { get; set; }

        private static async Task Main(string[] args)
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
                    ConfigureMassTransit(services);
                    services.AddHostedService<HostedService>();
                    services.AddHostedService<SimplePublisherService>();
                })
                .UseSerilog();
        }

        private void ConfigureMassTransit(IServiceCollection services)
        {
            IMassTransitTransport busTransport = new AzureServiceBusTransport(Configuration);

            //if (Environment.IsDevelopment())
            //    busTransport = new RabbitMqTransport();
            services.AddMassTransit(config =>
            {
                config.AddBus(ctx => busTransport.BusConfiguration);
            });
            
            //services.AddSingleton<IPublishEndpoint>(busTransport.BusConfiguration);
            //services.AddSingleton<ISendEndpointProvider>(busTransport.BusConfiguration);
            //services.AddSingleton<IBus>(busTransport.BusConfiguration);
        }
    }

    public interface IMassTransitTransport
    {
        public IBusControl BusConfiguration { get; }
    }

    public class AzureServiceBusTransport : IMassTransitTransport
    {
        private readonly HostSettings _azureHostSetting;
        private IBusControl _configuration;

        public AzureServiceBusTransport(IConfiguration configuration)
        {
            var uriString = configuration["AzureServiceBus:Connection"];
            var serviceUri = new Uri(uriString);
            _azureHostSetting = new HostSettings
            {
                ServiceUri = serviceUri,
                TransportType = TransportType.Amqp,
                TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider("RootManageSharedAccessKey",
                    configuration["AzureServiceBus:SharedKey"])
            };

            //Azure Service Bus
        }

        public IBusControl BusConfiguration => _configuration ??= ConfigureBus();

        private IBusControl ConfigureBus() =>
            Bus.Factory.CreateUsingAzureServiceBus(config =>
            {
                config.Host(_azureHostSetting);

                config.Send<ValueEntered>( x => { x.UseRoutingKeyFormatter<ValueEntered>(ctx => ctx.Message.Value );});
                config.Message<ValueEntered>(x => x.SetEntityName("value.entered"));
                config.Publish<ValueEntered>(x => { x.EnablePartitioning = true; });
            });
    }

    public class RabbitMqTransport : IMassTransitTransport
    {
        private IBusControl _butConfiguration;
        private readonly RabbitMqHostConfigurator _rabbitMqHostSettings;


        public RabbitMqTransport()
        {
            _rabbitMqHostSettings = new RabbitMqHostConfigurator("localhost", "/");
        }

        public IBusControl BusConfiguration => _butConfiguration ??= ConfigureBus();

        private IBusControl ConfigureBus() =>
            Bus.Factory.CreateUsingRabbitMq(config =>
                {
                    config.Host(_rabbitMqHostSettings.Settings);
                    config.Message<ValueEntered>(x => x.SetEntityName("value.entered"));
                    config.Publish<ValueEntered>(x => { x.ExchangeType = ExchangeType.Topic; });
                }
            );
    }
}