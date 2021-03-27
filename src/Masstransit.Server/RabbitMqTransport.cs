using MassTransit;
using MassTransit.RabbitMqTransport.Configurators;
using MassTransit.SharedTypes;
using RabbitMQ.Client;

namespace Masstransit.Publisher
{
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
            Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    cfg.Host(_rabbitMqHostSettings.Settings);

                    cfg.Message<SomeValue>(x => x.SetEntityName("value.entered"));
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