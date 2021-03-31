using MassTransit;

namespace Masstransit.Publisher
{
    public interface IMassTransitTransport
    {
        public IBusControl BusConfiguration { get; }
    }
}