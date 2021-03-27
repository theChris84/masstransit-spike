using System;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core.Configurators;
using MassTransit.SharedTypes;
using MassTransit.Topology;
using MassTransit.Topology.EntityNameFormatters;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;

namespace Masstransit.Publisher
{
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
                config.Message<SomeValue>(x =>
                {
                    //x.SetEntityNameFormatter(  new MessageEntityNameFormatter<SomeValue>( new ContractEntityFormatter() )  );
                    x.SetEntityName($"contracts-{nameof(SomeValue)}");
                });
                config.Publish<SomeValue>(x =>
                {
                    //x.RequiresDuplicateDetection = true;
                    //x.EnablePartitioning = true;
                });
                config.Send<SomeValue>(x =>
                {
                    x.UsePartitionKeyFormatter<SomeValue>(ctx => ctx.Message.RoutingKey);
                    x.UseSessionIdFormatter<SomeValue>( f => $"RoutingKey={f.Message.RoutingKey}");
                    x.UseRoutingKeyFormatter<SomeValue>(ctx => $"RoutingKey={ctx.Message.RoutingKey}");
                });
            });

        private class ContractEntityFormatter : IEntityNameFormatter
        {
            public string FormatEntityName<T>() =>
                $"contract-{typeof(T).Name}";
        }
    }
}