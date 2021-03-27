using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            //busTransport = new RabbitMqTransport();
            services.AddMassTransit(config =>
            {
                config.AddBus(ctx => busTransport.BusConfiguration);
            });

            services.AddSingleton<IPublishEndpoint>(busTransport.BusConfiguration);
            services.AddSingleton<ISendEndpointProvider>(busTransport.BusConfiguration);
            services.AddSingleton<IBus>(busTransport.BusConfiguration);
        }
    }
}