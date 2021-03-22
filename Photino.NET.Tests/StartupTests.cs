using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PhotinoNET;
using Xunit;

namespace Photino.NET.Tests
{
    public class StartupTests
    {
        public const string APP_NAME = "With Startup";

        [Fact]
        public void UseStartup_Configure()
        {
            var host = (new PhotinoHostBuilder(APP_NAME)
                    .ConfigureHostConfiguration(builder =>
                    {
                        builder.AddJsonFile(source =>
                        {
                            source.Optional = false;
                            source.Path = "appSettings.Test.json";
                        });
                    }) as PhotinoHostBuilder)?
                .WithStartup<Startup>()
                .Build() as PhotinoHost;

            host.Should().NotBeNull();
            host.Configuration.Should().NotBeNull();
            host.Configuration.GetSection("Logging").Should().NotBeNull();
            host.Configuration.GetSection("Logging").GetSection("Console").Should().NotBeNull();

            var logger = host.Services.GetService<ILogger<StartupTests>>();
            logger.Should().NotBeNull();
            logger.LogInformation(APP_NAME);
        }

        [Fact]
        public void UseStartup_ConfigureServices()
        {
            var host = (new PhotinoHostBuilder(APP_NAME)
                    .ConfigureHostConfiguration(builder =>
                    {
                        builder.AddJsonFile(source =>
                        {
                            source.Optional = false;
                            source.Path = "appSettings.Test.json";
                        });
                    }) as PhotinoHostBuilder)?
                .WithStartup<Startup>()
                .Build() as PhotinoHost;

            host.Should().NotBeNull();
            host.Configuration.Should().NotBeNull();
            host.Configuration.GetSection("Logging").Should().NotBeNull();
            host.Configuration.GetSection("Logging").GetSection("Console").Should().NotBeNull();

            var startup = host.Services.GetService<Startup>();
            startup.Should().NotBeNull();
            startup.Configuration.Should().NotBeNull();
            startup.Configuration.Should().BeSameAs(host.HostConfiguration);
        }
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(this);
        }

        public void Configure(IHostBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.Properties["IsDevelopment"] = true;
            }
            else
            {
                app.Properties["IsDevelopment"] = false;
            }

            app.ConfigureLogging(builder =>
            {
                builder
                    .AddConsole(options =>
                    {
                        Configuration.Bind(options);
                    })
                    .Configure(options =>
                    {
                        options.ActivityTrackingOptions = ActivityTrackingOptions.None;
                    });
            });
        }
    }
}