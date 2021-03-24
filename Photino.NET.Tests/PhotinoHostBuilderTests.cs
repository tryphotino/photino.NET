using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PhotinoNET;
using Xunit;

namespace Photino.NET.Tests
{
    public class PhotinoHostBuilderTests
    {
        private const string APP_NAME = "Title";

        [Fact]
        public void ConfigureHostConfiguration_ValidJson()
        {
            var builder = new PhotinoHostBuilder<Startup>(APP_NAME);

            builder.Should().NotBeNull();
            builder.Title.Should().Be(APP_NAME);

            builder.ConfigureHostConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddJsonFile(source =>
                {
                    source.Optional = false;
                    source.Path = "appSettings.Test.json";
                });

                var hostConfiguration = configurationBuilder.Build();
            
                hostConfiguration.Should().NotBeNull();
                hostConfiguration.GetChildren().Should().NotBeEmpty();
                hostConfiguration.GetSection("AppSettings").Should().NotBeNull();
                hostConfiguration.GetSection("AppSettings").GetChildren().Should().NotBeEmpty();
                hostConfiguration.GetSection("AppSettings").GetChildren().First().Key.Should().Be("Value1");
                hostConfiguration.GetSection("AppSettings").GetChildren().First().Value.Should().Be("1");
            });
        }

        [Fact]
        public void ConfigureAppConfiguration_ValidJson()
        {
            var builder = new PhotinoHostBuilder<Startup>(APP_NAME);

            builder.Should().NotBeNull();
            builder.Title.Should().Be(APP_NAME);

            builder.ConfigureAppConfiguration((context, configurationBuilder) =>
            {
                context.Should().NotBeNull();

                configurationBuilder.AddJsonFile(source =>
                {
                    source.Optional = false;
                    source.Path = "appSettings.Test.json";
                });
            });

            var host = builder.Build();

            var configuration = host.Services.GetService<IConfiguration>();
            configuration.Should().NotBeNull();
            configuration.GetChildren().Should().NotBeEmpty();
            configuration.GetSection("AppSettings").Should().NotBeNull();
            configuration.GetSection("AppSettings").GetChildren().Should().NotBeEmpty();
            configuration.GetSection("AppSettings").GetChildren().First().Key.Should().Be("Value1");
            configuration.GetSection("AppSettings").GetChildren().First().Value.Should().Be("1");
        }

        [Fact]
        public void ConfigureContainer_Object()
        {
            var builder = new PhotinoHostBuilder<Startup>(APP_NAME);

            builder.Should().NotBeNull();
            builder.Title.Should().Be(APP_NAME);

            builder.ConfigureContainer<object>((context, containerBuilder) =>
            {
                context.Should().NotBeNull();
                context.HostingEnvironment.Should().NotBeNull();
                containerBuilder.Should().NotBeNull();
            });

            var host = builder.Build();

            host.Should().NotBeNull();
        }

        [Fact]
        public Task Start_Host()
        {
            var mre = new ManualResetEventSlim();

            var location = new Point(100, 100);
            var size = new Size(500, 250);
            var builder = new PhotinoHostBuilder<Startup>(APP_NAME)
                .WithIsFullscreen(false)
                .WithPosition(location)
                .WithSize(size)
                .WithOptions(options =>
                {
                    options.WindowCreatedHandler += (sender, args) =>
                    {
                        var window = sender as PhotinoWindow;

                        window.Should().NotBeNull();

                        window.Location.Should().BeEquivalentTo(location);
                        window.Size.Should().BeEquivalentTo(size);

                        mre.Set();
                    };
                });

            builder.Should().NotBeNull();
            builder.Title.Should().Be(APP_NAME);

            builder.ConfigureContainer<object>((context, containerBuilder) =>
            {
                context.Should().NotBeNull();
                context.HostingEnvironment.Should().NotBeNull();
                containerBuilder.Should().NotBeNull();
            });

            using var host = builder.Build();

            host.Should().NotBeNull();

            var task = host.StartAsync();

            task.IsCompleted.Should().BeTrue();
            task.Exception.Should().BeNull();
            task.IsFaulted.Should().BeFalse();
            task.IsCanceled.Should().BeFalse();
            task.IsCompletedSuccessfully.Should().BeTrue();

            var photinoWindow = host.Services.GetService<PhotinoWindow>();

            photinoWindow.Should().NotBeNull();
            photinoWindow.Title.Should().Be(APP_NAME);

            mre.Wait();

            return task;
        }

        [Fact]
        public Task Stop_Host()
        {
            var mre = new ManualResetEventSlim();

            var location = new Point(100, 100);
            var size = new Size(500, 250);
            var builder = new PhotinoHostBuilder<Startup>(APP_NAME)
                .WithIsFullscreen(false)
                .WithPosition(location)
                .WithSize(size)
                .WithOptions(options =>
                {
                    options.SizeChangedHandler += (sender, args) =>
                    {
                        var window = sender as PhotinoWindow;

                        window.Should().NotBeNull();

                        window.Location.Should().BeEquivalentTo(location);
                        window.Size.Should().BeEquivalentTo(size);

                        mre.Set();
                    };
                });

            builder.Should().NotBeNull();
            builder.Title.Should().Be(APP_NAME);

            builder.ConfigureContainer<object>((context, containerBuilder) =>
            {
                context.Should().NotBeNull();
                context.HostingEnvironment.Should().NotBeNull();
                containerBuilder.Should().NotBeNull();
            });

            using var host = builder.Build();

            host.Should().NotBeNull();

            var task = host.StartAsync();

            task.IsCompleted.Should().BeTrue();
            task.Exception.Should().BeNull();
            task.IsFaulted.Should().BeFalse();
            task.IsCanceled.Should().BeFalse();
            task.IsCompletedSuccessfully.Should().BeTrue();

            var photinoWindow = host.Services.GetService<PhotinoWindow>();
            photinoWindow.Should().NotBeNull();
            photinoWindow.Title.Should().Be(APP_NAME);
            photinoWindow.Show();

            mre.Wait();

            task = host.StopAsync();

            task.IsCompleted.Should().BeTrue();
            task.Exception.Should().BeNull();
            task.IsFaulted.Should().BeFalse();
            task.IsCanceled.Should().BeFalse();
            task.IsCompletedSuccessfully.Should().BeTrue();

            return task;
        }
    }
}