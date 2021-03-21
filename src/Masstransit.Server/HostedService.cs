using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Masstransit.Publisher
{
    internal class HostedService : BackgroundService
    {
        private readonly IBusControl _bustControl;

        public HostedService(IBusControl bustControl)
        {
            _bustControl = bustControl;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => 
            _bustControl.StartAsync(stoppingToken);

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            base.StopAsync(cancellationToken);
            return base.StopAsync(cancellationToken);
        }
    }
}