using MassTransit.Topology;

namespace Masstransit.Publisher
{
    internal class CustomEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() =>
            $"Contracts-{typeof(T).Name}";
    }
}