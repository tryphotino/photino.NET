using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace PhotinoNET
{
    public class PhotinoHostedService<TStartup> : IHostedService, IDisposable
    {
        internal PhotinoHostedService(IServiceProvider services)
        {
            Startup = services.GetService<TStartup>();

            Services = services;

            Configuration = services.GetService<IConfiguration>();

            Window = services.GetService<PhotinoWindow>();
        }

        public void Dispose()
        {
            if (Startup is IHostedService hosted)
            {
                hosted.StopAsync(CancellationToken.None);
            }

            if (Startup is IDisposable disposable)
            {
                disposable.Dispose();
            }

            Window?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (Startup is IHostedService hostedStartup)
            {
                hostedStartup.StartAsync(CancellationToken.None);
            }

            Window = Window?.Show();

            return (Window != null)
                ? Task.CompletedTask
                : Task.FromException(new ApplicationException("Could not start window."));
        }

        public Task StopAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Window?.Close();            

            if (Startup is IHostedService hostedStartup)
            {
                hostedStartup.StopAsync(CancellationToken.None);
            }

            return (Window != null)
                ? Task.CompletedTask
                : Task.FromException(new ApplicationException("Window is null."));
        }

        internal TStartup Startup { get; }

        public IServiceProvider Services { get; }

        public IConfiguration Configuration { get; }

        public PhotinoWindow Window { get; private set; }
    }
}