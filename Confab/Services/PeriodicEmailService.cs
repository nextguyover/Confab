namespace Confab.Services
{
    using Confab.Data;
    using Confab.Services.Interfaces;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class PeriodicEmailService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public PeriodicEmailService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                DataContext context = scope.ServiceProvider.GetRequiredService<DataContext>();
                IEmailService emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.CheckAndSendModQueueReminder(context);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
