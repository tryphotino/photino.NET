using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
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
            var builder = new PhotinoHostBuilder(APP_NAME);

            builder.Should().NotBeNull();
            builder.Title.Should().Be(APP_NAME);

            builder.ConfigureHostConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddJsonFile(source =>
                {
                    source.Optional = false;
                    source.Path = "appSettings.Test.json";
                });
            });

            var host = builder.Build() as PhotinoHost;

            host.HostConfiguration.Should().NotBeNull();
            host.HostConfiguration.GetChildren().Should().NotBeEmpty();
            host.HostConfiguration.GetSection("AppSettings").Should().NotBeNull();
            host.HostConfiguration.GetSection("AppSettings").GetChildren().Should().NotBeEmpty();
            host.HostConfiguration.GetSection("AppSettings").GetChildren().First().Key.Should().Be("Value1");
            host.HostConfiguration.GetSection("AppSettings").GetChildren().First().Value.Should().Be("1");
        }

        [Fact]
        public void ConfigureAppConfiguration_ValidJson()
        {
            var builder = new PhotinoHostBuilder(APP_NAME);

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

            var host = builder.Build() as PhotinoHost;

            host.Configuration.Should().NotBeNull();
            host.Configuration.GetChildren().Should().NotBeEmpty();
            host.Configuration.GetSection("AppSettings").Should().NotBeNull();
            host.Configuration.GetSection("AppSettings").GetChildren().Should().NotBeEmpty();
            host.Configuration.GetSection("AppSettings").GetChildren().First().Key.Should().Be("Value1");
            host.Configuration.GetSection("AppSettings").GetChildren().First().Value.Should().Be("1");
        }

        [Fact]
        public void ConfigureContainer_Object()
        {
            var builder = new PhotinoHostBuilder(APP_NAME);

            builder.Should().NotBeNull();
            builder.Title.Should().Be(APP_NAME);

            builder.ConfigureContainer<object>((context, containerBuilder) =>
            {
                context.Should().NotBeNull();
                context.HostingEnvironment.Should().NotBeNull();
                context.HostingEnvironment.ApplicationName.Should().Be(APP_NAME);
                containerBuilder.Should().NotBeNull();
            });

            var host = builder.Build() as PhotinoHost;

            host.Should().NotBeNull();
        }

        [Fact]
        public Task Start_Host()
        {
            var mre = new ManualResetEventSlim();

            var location = new Point(100, 100);
            var size = new Size(500, 250);
            var builder = new PhotinoHostBuilder(APP_NAME)
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
                context.HostingEnvironment.ApplicationName.Should().Be(APP_NAME);
                containerBuilder.Should().NotBeNull();
            });

            using var host = builder.Build() as PhotinoHost;

            host.Should().NotBeNull();

            var task = host.StartAsync();

            task.IsCompleted.Should().BeTrue();
            task.Exception.Should().BeNull();
            task.IsFaulted.Should().BeFalse();
            task.IsCanceled.Should().BeFalse();
            task.IsCompletedSuccessfully.Should().BeTrue();

            host.Window.Should().NotBeNull();
            host.Window.Title.Should().Be(APP_NAME);

            mre.Wait();

            return task;
        }

        [Fact]
        public Task Stop_Host()
        {
            var mre = new ManualResetEventSlim();

            var location = new Point(100, 100);
            var size = new Size(500, 250);
            var builder = new PhotinoHostBuilder(APP_NAME)
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
                context.HostingEnvironment.ApplicationName.Should().Be(APP_NAME);
                containerBuilder.Should().NotBeNull();
            });

            using var host = builder.Build() as PhotinoHost;

            host.Should().NotBeNull();

            var task = host.StartAsync();

            task.IsCompleted.Should().BeTrue();
            task.Exception.Should().BeNull();
            task.IsFaulted.Should().BeFalse();
            task.IsCanceled.Should().BeFalse();
            task.IsCompletedSuccessfully.Should().BeTrue();

            host.Window.Should().NotBeNull();
            host.Window.Title.Should().Be(APP_NAME);

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