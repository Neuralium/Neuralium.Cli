using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Neuralium.Cli.Classes.API;
using Neuralium.Cli.Classes.Runtime;
using Serilog;

namespace Neuralium.Cli {
	public class Program {

		public static async Task<int> Main(string[] args) {
			//options parsing first
			var result = new Parser(with => with.EnableDashDash = true).ParseArguments<QueryOptions, InteractiveOptions>(args);

			return await result.MapResult(async (InteractiveOptions options) => await RunProgram(options), async (QueryOptions opts) => await RunAndReturnExitCode(opts), HandleParseError);
		}

		private static async Task<int> RunProgram(InteractiveOptions cmdQueryOptions) {
			Bootstrap boostrapper = new Bootstrap();
			boostrapper.SetCmdOptions(cmdQueryOptions);

			return await boostrapper.Run();
		}

		private static Task<int> HandleParseError(IEnumerable<Error> errors) {

			return Task.FromResult(-1);
		}

		private static async Task<int> RunAndReturnExitCode(QueryOptions opts) {

			IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("config.json", true, true);

			IConfigurationRoot configuration = builder.Build();

			AppSettings appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();

			if(appSettings == null) {
				appSettings = new AppSettings();
			}

			Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

			var api = new NeuraliumApi<IApiMethods>();
			api.Init(appSettings, NeuraliumApi.UseModes.SendOnly);

			await api.Connect();

			try {
				string result = await api.InvokeMethod(opts.Operation, opts.Parameters);

				if(!string.IsNullOrWhiteSpace(result)) {
					Log.Information(result);
				} else {
					Log.Information("returned");
				}
			} catch(Exception ex) {
				Log.Error(ex, "Failed to query method.");
			}

			await api.Disconnect();

			Log.CloseAndFlush();

			return -1;
		}
	}
}