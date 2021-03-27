using MassTransit;
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
    internal class Program
    {
        private IHostEnvironment Environment { get;  set; }
        private IConfigurationRoot Configuration { get; set; }

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
                        .AddEnvironmentVariables()
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
                    services.AddHostedService<HostedService>();
                    services.AddHostedService<SimplePublisherService>();
                })
                .UseSerilog();

        private void ConfigureMassTransit(IServiceCollection services, IConfiguration configuration)
        {
            IMassTransitTransport busTransport = new AzureServiceBusTransport(configuration);

            if (Environment.IsDevelopment())
                 busTransport = new RabbitMqTransport(configuration);

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