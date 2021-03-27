using MassTransit;
using MassTransit.RabbitMqTransport.Configurators;
using MassTransit.SharedTypes;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Masstransit.Publisher
{
    public class RabbitMqTransport : IMassTransitTransport
    {
        private IBusControl _butConfiguration;
        private readonly RabbitMqHostConfigurator _rabbitMqHostSettings;

        public RabbitMqTransport(IConfiguration configuration)
        {
            var url = configuration["RabbitMq:Connection"];
            _rabbitMqHostSettings = new RabbitMqHostConfigurator(url, "/");
        }

        public IBusControl BusConfiguration => _butConfiguration ??= ConfigureBus();

        private IBusControl ConfigureBus() =>
            Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    cfg.Host(_rabbitMqHostSettings.Settings);

                    cfg.Message<SomeValue>(x => x.SetEntityName($"contracts-{nameof(SomeValue)}"));
                    cfg.Publish<SomeValue>(x =>
                    {
                        x.ExchangeType = ExchangeType.Topic;
                    });
                    cfg.Send<SomeValue>(x =>
                    {
                        x.UseRoutingKeyFormatter<SomeValue>(ctx => ctx.Message.RoutingKey.ToLower());
                    });

                }
            );
    }
}