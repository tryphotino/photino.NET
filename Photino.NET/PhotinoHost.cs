using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PhotinoNET
{
    public class PhotinoHost : IHost
    {
        private PhotinoWindow _window;
        private IServiceProvider _services;
        private object _startup;

        internal PhotinoHost(PhotinoHostBuilder builder, object startup)
        {
            _startup = startup;

            _services = builder.Services;

            HostConfiguration = builder.HostConfiguration;
            Configuration = builder.AppConfiguration;

            _window = new PhotinoWindow(
                builder.Title, 
                builder.Options, 
                builder.Width, 
                builder.Height,
                builder.Left, 
                builder.Top, 
                builder.Fullscreen);
        }

        public IConfigurationRoot HostConfiguration { get; set; }

        public void Dispose()
        {
            _window?.Dispose();

            if (_startup is IHostedService hosted)
            {
                hosted.StopAsync(CancellationToken.None);
            }

            if (_startup is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _services = null;
            _startup = null;
        }

        public Task StartAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            if (_startup is IHostedService hostedStartup)
            {
                hostedStartup.StartAsync(CancellationToken.None);
            }

            _window = _window?.Show();

            return (_window != null)
                ? Task.CompletedTask
                : Task.FromException(new ApplicationException("Could not start window."));
        }

        public Task StopAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _window?.Close();            

            if (_startup is IHostedService hostedStartup)
            {
                hostedStartup.StopAsync(CancellationToken.None);
            }

            return (_window != null)
                ? Task.CompletedTask
                : Task.FromException(new ApplicationException("Window is null."));
        }

        public IServiceProvider Services => _services;

        public IConfiguration Configuration { get; internal set; }

        public PhotinoWindow Window => _window;
    }
}