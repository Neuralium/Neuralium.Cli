using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Neuralium.Cli.Classes;
using Neuralium.Cli.Classes.API;
using Neuralium.Cli.Classes.Runtime;
using Serilog;

namespace Neuralium.Cli {
	public class Program {

		protected const string appsettings = "config/config.json";
		protected const string docker_base_path = "/home/data/config.json";
		protected const string docker_appsettings = "config/docker.config.json";
		
		public static async Task<int> Main(string[] args) {
			//options parsing first
			var result = new Parser(with => with.EnableDashDash = true).ParseArguments<RunOptions, InteractiveOptions>(args);

			return await result.MapResult(async (InteractiveOptions options) => await RunProgram(options), async (RunOptions opts) => await RunAndReturnExitCode(opts), HandleParseError);
		}

		private static async Task<int> RunProgram(InteractiveOptions cmdQueryOptions) {
			Bootstrap boostrapper = new Bootstrap();
			boostrapper.SetCmdOptions(cmdQueryOptions);

			return await boostrapper.Run();
		}

		private static Task<int> HandleParseError(IEnumerable<Error> errors) {

			foreach(var error in errors) {
				if(error is SetValueExceptionError svex) {
					Console.WriteLine($"value: {svex.NameInfo.NameText} - {svex.Exception}");
				} else {
					Console.WriteLine($"{error.Tag}- {error.StopsProcessing}");
				}
			}
			
			return Task.FromResult(-1);
		}

		private static async Task<int> RunAndReturnExitCode(RunOptions opts) {

			IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
				
			if(opts.RuntimeMode.ToUpper() == "DOCKER") {
				Console.WriteLine($"Docker mode.");
				if(File.Exists(docker_base_path)) {
					Console.WriteLine($"Loading config file {docker_base_path}");
					builder = builder.AddJsonFile(docker_base_path, false, false);
				} else {
					Console.WriteLine($"Default docker config not found. Loading config file {docker_appsettings}");
					builder = builder.AddJsonFile(docker_appsettings, false, false);
				}
			} else {
				builder = builder.AddJsonFile(appsettings, false, false);
			}

			IConfigurationRoot configuration = builder.Build();

			AppSettings appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();

			if(appSettings == null) {
				appSettings = new AppSettings();
			}

			Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();

			var api = new NeuraliumApi<IApiMethods>();

			IQueryJson parameters = PrepareQueryJson(opts);
			
			api.Init(appSettings, opts, NeuraliumApi.UseModes.SendOnly);

			await api.Connect();

			try {
				string result = await api.InvokeMethod(parameters);

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

		private static IQueryJson PrepareQueryJson(RunOptions opts) {
			

			if(!string.IsNullOrWhiteSpace(opts.ConfigurationFile)) {
				if(!File.Exists(opts.ConfigurationFile)) {
					throw new ApplicationException("Configuration file does not exist.");
				}

				string json = File.ReadAllText(opts.ConfigurationFile);

				return System.Text.Json.JsonSerializer.Deserialize<QueryJsonNamed>(json);
			} else {
				IQueryJson result = null;
				
				if(!string.IsNullOrWhiteSpace(opts.JParameters)) {
					result = new QueryJsonNamed();
					((QueryJsonNamed)result).Parameters = System.Text.Json.JsonSerializer.Deserialize<List<QueryJsonNamed.NamedOperationParameters>>(opts.JParameters);
				}
				else if(opts.Parameters.Any()) {

					result = new QueryJsonIndexed();
					var list = opts.Parameters.ToList();
					for(int i = 0; i < opts.Parameters.Count(); i++) {
						
						((QueryJsonIndexed)result).Parameters.Add(new QueryJsonIndexed.IndexedOperationParameters(i, list[i]));
					}
				} else {
					result = new QueryJsonIndexed();
				} 
				
				if(!string.IsNullOrWhiteSpace(opts.Operation)) {
					result.Operation = opts.Operation;
				}

				return result;
			}
		}
	}
}