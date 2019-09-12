using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neuralium.Cli.Classes.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace Neuralium.Cli.Classes.Runtime {
	public class Bootstrap {

		private const string _prefix = "NEURALIUM_";
		private const string _appsettings = "config.json";
		private const string _hostsettings = "hostsettings.json";
		protected InteractiveOptions CmdQueryOptions;

		protected virtual void ConfigureExtraServices(IServiceCollection services, IConfiguration configuration) {
			services.AddHostedService<CliService>();
			services.AddSingleton<ICliApp, CliApp<NeuraliumApi<IApiMethods>, IApiMethods>>();
		}

		public void SetCmdOptions(InteractiveOptions cmdQueryOptions) {
			this.CmdQueryOptions = cmdQueryOptions;
		}

		public static string GetExecutingDirectoryName() {
			Uri location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);

			DirectoryInfo directoryInfo = new FileInfo(location.AbsolutePath).Directory;

			if(directoryInfo != null) {
				return directoryInfo.FullName;
			}

			throw new ApplicationException("Invalid execution directory");
		}

		protected virtual void BuildConfiguration(HostBuilderContext hostingContext, IConfigurationBuilder configApp) {

		}

		public async Task<int> Run() {

			IHostBuilder host = new HostBuilder().ConfigureHostConfiguration(configHost => {
				//					configHost.SetBasePath(GetExecutingDirectoryName());
				//					configHost.AddJsonFile(_hostsettings, optional: true);
				//					configHost.AddEnvironmentVariables(prefix: _prefix);
			}).ConfigureAppConfiguration((hostingContext, configApp) => {

				configApp.SetBasePath(GetExecutingDirectoryName()).AddJsonFile(_appsettings, false, false).AddEnvironmentVariables().AddEnvironmentVariables(_prefix);

				this.BuildConfiguration(hostingContext, configApp);
			}).ConfigureServices((hostContext, services) => {
				services.AddOptions<HostOptions>().Configure(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(10));

				services.Configure<MvcJsonOptions>(options => {
					options.SerializerSettings.Converters.Add(new StringEnumConverter());
					options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
				});

				string configSection = "AppSettings";

				Log.Verbose($"Loading config section {configSection}");

				services.Configure<AppSettings>(hostContext.Configuration.GetSection(configSection));

				services.AddSingleton<InteractiveOptions, InteractiveOptions>(x => this.CmdQueryOptions);

				// Add extra static services here

				// allow children to add their own overridable services
				this.ConfigureExtraServices(services, hostContext.Configuration);
			}).ConfigureLogging((hostingContext, logging) => {

				logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
				logging.AddConsole();
			}).UseSerilog((hostingContext, loggerConfiguration) => {
				loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
			}).UseConsoleLifetime();

			try {
				Log.Information("Starting host");
				await host.RunConsoleAsync();

				return 0;
			} catch(Exception ex) {
				Log.Fatal(ex, "Host terminated unexpectedly");

				return 1;
			} finally {
				Log.CloseAndFlush();
			}

		}
	}
}