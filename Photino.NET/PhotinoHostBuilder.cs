using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace PhotinoNET
{
    public class PhotinoHostBuilder : IHostBuilder
    {
        private Type _startType;
        private IConfigurationRoot _hostConfigurationRoot;
        private IConfigurationRoot _appConfiguration;
        private IServiceProvider _services;
        private Point _point;
        private Size _size;
        private bool _isFs;

        private Action<PhotinoWindowOptions> _options;

        private readonly IDictionary<object, object> _properties = 
            new Dictionary<object, object>();

        private ConfigurationBuilder _hostConfigurationBuilder;
        private ConfigurationBuilder _appConfigurationBuilder;
        private readonly HostBuilderContext _hostBuilderContext;
        private ServiceCollection _serviceCollection;

        public PhotinoHostBuilder(string title)
        {
            _hostBuilderContext = new HostBuilderContext(Properties)
            {
                HostingEnvironment = new HostingEnvironment
                {
                    ApplicationName = title,
                    ContentRootPath = Directory.GetCurrentDirectory(),
                    ContentRootFileProvider = new PhysicalFileProvider(
                        Directory.GetCurrentDirectory())
                }
            };

            _serviceCollection = new ServiceCollection();

            Title = title;
        }

        public Type StartupType => _startType; 

        public string Title { get; }

        public int Width => _size.Width;

        public int Height => _size.Height;

        public int Left => _point.X;

        public int Top => _point.Y;

        public bool Fullscreen => _isFs;

        public IServiceProvider Services => _services;

        public Action<PhotinoWindowOptions> Options => _options;

        public override string ToString()
        {
            return Title;
        }

        public IDictionary<object, object> Properties => _properties;

        public IConfigurationRoot HostConfiguration => _hostConfigurationRoot;

        public IConfigurationRoot AppConfiguration => _appConfiguration;

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            _hostConfigurationBuilder ??= new ConfigurationBuilder();
            configureDelegate(_hostConfigurationBuilder);

            return this;
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _appConfigurationBuilder ??= new ConfigurationBuilder();
            configureDelegate(_hostBuilderContext, _appConfigurationBuilder);

            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            configureDelegate(_hostBuilderContext, _serviceCollection);

            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
            IServiceProviderFactory<TContainerBuilder> factory)
        {
            var builder = factory.CreateBuilder(_serviceCollection);
            
            _services = factory.CreateServiceProvider(builder);

            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
            Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        {
            var fac = factory(_hostBuilderContext);

            var builder = fac.CreateBuilder(_serviceCollection);
            
            _services = fac.CreateServiceProvider(builder);

            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(
            Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            var builder = Activator.CreateInstance<TContainerBuilder>();
            configureDelegate(_hostBuilderContext, builder);

            return this;
        }

        public PhotinoHostBuilder WithPosition(Point point)
        {
            this._point = point;

            return this;
        }

        public PhotinoHostBuilder WithOptions(Action<PhotinoWindowOptions> options)
        {
            this._options = options;

            return this;
        }

        public PhotinoHostBuilder WithSize(Size size)
        {
            this._size = size;

            return this;
        }

        public PhotinoHostBuilder WithIsFullscreen(bool isFs)
        {
            this._isFs = isFs;

            return this;
        }

        public PhotinoHostBuilder WithStartup<TStart>()
        {
            this._startType = typeof(TStart);

            return this;
        }

        public IHost Build()
        {
            _hostConfigurationRoot = _hostConfigurationBuilder?.Build() ?? 
                                     new ConfigurationRoot(new List<IConfigurationProvider>());
            
            dynamic startup = null;

            if (StartupType != null)
            {
                startup = Activator.CreateInstance(StartupType, _hostConfigurationRoot) ?? 
                          Activator.CreateInstance(StartupType);
            }

            try
            {
                startup?.ConfigureServices(_serviceCollection);
            }
            catch
            {
                // ignore 
            }

            try
            {
                startup?.Configure(this, _hostBuilderContext.HostingEnvironment);
            }
            catch
            {
                // ignore 
            }

            _appConfiguration = (_appConfigurationBuilder ?? new ConfigurationBuilder()).Build();

            _services ??= (_serviceCollection ?? new ServiceCollection()).BuildServiceProvider();

            return new PhotinoHost(this, startup);
        }
    }
}