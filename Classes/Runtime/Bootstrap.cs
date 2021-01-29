using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neuralia.Blockchains.Core;
using Neuralia.Blockchains.Core.Logging;
using Neuralium.Cli.Classes.API;
using Serilog;





















namespace Neuralium.Cli.Classes.Runtime {
	public class Bootstrap {

		private const string _prefix = "NEURALIUM_";
		private const string _appsettings = "config/config.json";
		private const string _docker_base_path = "/home/data/config.json";
		private const string _docker_appsettings = "config/docker.config.json";
		private const string _hostsettings = "hostsettings.json";
		protected OptionsBase CmdQueryOptions;

		protected virtual void ConfigureExtraServices(IServiceCollection services, IConfiguration configuration)
		{
			services.AddHostedService<CliService>();
			services.AddSingleton<ICliApp, CliApp<NeuraliumApi<IApiMethods>, IApiMethods>>();
		}

		public void SetCmdOptions(OptionsBase cmdQueryOptions)
		{
			this.CmdQueryOptions = cmdQueryOptions;
		}

		public static string GetExecutingDirectoryName()
		{
			return FileUtilities.GetExecutingDirectory();

		}
		public static IConfigurationRoot BuildConfiguration(OptionsBase opts)
		{
			IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());

			if(opts.RuntimeMode.ToUpper() == "DOCKER") {
				Console.WriteLine("Docker mode.");

				if (File.Exists(_docker_base_path))
				{
					Console.WriteLine($"Loading config file {_docker_base_path}");
					builder = builder.AddJsonFile(_docker_base_path, false, false);
				}
				else
				{
					Console.WriteLine($"Default docker config not found. Loading config file {_docker_appsettings}");
					builder = builder.AddJsonFile(_docker_appsettings, false, false);
				}
			} else {
				builder = builder.AddJsonFile(_appsettings, false, false);
			}

			return builder.Build();
			
		}

		protected virtual void BuildConfiguration(HostBuilderContext hostingContext, IConfigurationBuilder configApp) {
			//Not sure how to re-use the 'IConfigurationRoot BuildConfiguration(OptionsBase)' method above that is currently used in the stateless, shoot'n'forget code path
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

				string configSection = "AppSettings";

				NLog.Default.Verbose($"Loading config section {configSection}");

				services.Configure<AppSettings>(hostContext.Configuration.GetSection(configSection));

				services.AddSingleton<OptionsBase, OptionsBase>(x => this.CmdQueryOptions);

				// Add extra static services here

				// allow children to add their own overridable services
				this.ConfigureExtraServices(services, hostContext.Configuration);
			}).ConfigureLogging((hostingContext, logging) => {
				Console.WriteLine($"logging section: {hostingContext.Configuration.GetSection("Logging")}");
				logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
				logging.AddConsole();
			}).UseSerilog((hostingContext, loggerConfiguration) => {
				loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
			}).UseConsoleLifetime();

			try {
				NLog.Default.Information("Starting host");
				await host.RunConsoleAsync().ConfigureAwait(false);

				return 0;
			} catch(Exception ex) {
				NLog.Default.Fatal(ex, "Host terminated unexpectedly");

				return 1;
			} finally {
				Log.CloseAndFlush();
			}

		}
	}
}