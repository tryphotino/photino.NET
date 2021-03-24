using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace PhotinoNET
{
    public class PhotinoHostBuilder<TStartup> : IHostBuilder
        where TStartup : class
    {
        private TStartup _startup;

        private Point _point;
        private Size _size;
        private bool _isFs;
        private Action<PhotinoWindowOptions> _options;

        private readonly IHostBuilder _builder;

        public PhotinoHostBuilder(string title)
        {
            _builder = Host.CreateDefaultBuilder()
                .ConfigureServices(collection =>
                {
                    collection.AddSingleton(provider =>
                        new PhotinoWindow(Title, Options, Width, Height, Left, Top, Fullscreen)
                    );

                    collection.AddSingleton<PhotinoHostedService<TStartup>>();
                });

            Title = title;
        }

        public string Title { get; }

        private int Width => _size.Width;

        private int Height => _size.Height;

        private int Left => _point.X;

        private int Top => _point.Y;

        private bool Fullscreen => _isFs;

        private Action<PhotinoWindowOptions> Options => _options;

        public override string ToString()
        {
            return Title;
        }

        public IDictionary<object, object> Properties => _builder.Properties;

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _builder.ConfigureHostConfiguration(configureDelegate);

            return this;
        }

        private void ConfigureHostConfiguration(IConfigurationBuilder builder)
        {
            var hostConfigurationRoot = builder.Build();

            try
            {
                var ctors = typeof(TStartup).GetConstructors();

                var ctorWithConfig = ctors.FirstOrDefault(c =>
                    c.GetParameters().Length == 1 &&
                    c.GetParameters()
                        .FirstOrDefault(pi =>
                            pi?.ParameterType?.FullName?.Equals(typeof(IConfiguration).FullName) ?? false) != null);

                if (ctorWithConfig != null)
                {
                    _startup = (TStartup) ctorWithConfig.Invoke(new object[] {hostConfigurationRoot});
                }
                else
                {
                    _startup = Activator.CreateInstance<TStartup>();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Could not create instance of {typeof(TStartup)}.", ex);
            }
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _builder.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        private void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder _)
        {
            if (_startup is null) return;

            try
            {
                var methodInfo =
                    typeof(TStartup).GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance);

                if (methodInfo != null)
                {
                    methodInfo.Invoke(_startup, new object[] {this, context.HostingEnvironment});
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"{typeof(TStartup).Name}.Configure could not be invoked.{Environment.NewLine}{ex}");
            }
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            _builder.ConfigureServices(configureDelegate);

            return this;
        }

        private void ConfigureServices(HostBuilderContext context, IServiceCollection serviceCollection)
        {
            if (_startup is null) return;

            serviceCollection.AddSingleton(_startup);

            try
            {
                var methodInfo =
                    typeof(TStartup).GetMethod("ConfigureServices", BindingFlags.Public | BindingFlags.Instance);

                if (methodInfo != null)
                {
                    methodInfo.Invoke(_startup, new object[] {serviceCollection});
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"{typeof(TStartup).Name}.ConfigureServices could not be invoked.{Environment.NewLine}{ex}");
            }
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
            IServiceProviderFactory<TContainerBuilder> factory)
        {
            _builder.UseServiceProviderFactory(factory);

            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
            Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        {
            _builder.UseServiceProviderFactory(factory);

            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(
            Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            _builder.ConfigureContainer(configureDelegate);

            return this;
        }

        public PhotinoHostBuilder<TStartup> WithPosition(Point point)
        {
            this._point = point;

            return this;
        }

        public PhotinoHostBuilder<TStartup> WithOptions(Action<PhotinoWindowOptions> options)
        {
            this._options = options;

            return this;
        }

        public PhotinoHostBuilder<TStartup> WithSize(Size size)
        {
            this._size = size;

            return this;
        }

        public PhotinoHostBuilder<TStartup> WithIsFullscreen(bool isFs)
        {
            this._isFs = isFs;

            return this;
        }

        public IHost Build()
        {
            _builder.ConfigureHostConfiguration(ConfigureHostConfiguration);
            _builder.ConfigureAppConfiguration(ConfigureAppConfiguration);
            _builder.ConfigureServices(ConfigureServices);

            var host = _builder.Build();

            return host;
        }
    }
}