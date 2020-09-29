using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Neuralia.Blockchains.Core.Logging;
using Neuralium.Cli.Classes;
using Neuralium.Cli.Classes.API;
using Neuralium.Cli.Classes.Runtime;
using Serilog;
using Neuralia.NClap;
using Neuralia.NClap.Metadata;
using Neuralium.Cli.Classes.Runtime.Commands;

namespace Neuralium.Cli {
	public class Program {

		protected const string appsettings = "config/config.json";
		protected const string docker_base_path = "/home/data/config.json";
		protected const string docker_appsettings = "config/docker.config.json";

		public static async Task<int> Main(string[] args) {
			//options parsing first
			if (CommandLineParser.TryParse(args, out OptionsBase programArgs))
			{

				if (programArgs.ApiCommand == null) //interactive mode
				{
					Bootstrap boot = new Bootstrap();
					
					boot.SetCmdOptions(programArgs);
				
					return await boot.Run().ConfigureAwait(false);
				}
				
				if (programArgs.ApiCommand.InstantiatedCommand is
					CommandBase<NeuraliumApi<IApiMethods>, IApiMethods> command)
				{
					var configuration = Bootstrap.BuildConfiguration(programArgs);
					
					AppSettings appSettings = configuration.GetSection("AppSettings").Get<AppSettings>() ?? new AppSettings();
					
					Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
					
					NeuraliumApi<IApiMethods> api = new NeuraliumApi<IApiMethods>();
					
					api.Init(appSettings, programArgs, NeuraliumApi.UseModes.SendReceive);
					
					await api.Connect().ConfigureAwait(false);
					
					command.Api = api;
					command.Arguments = programArgs;
					CommandResult result = command.ExecuteAsync(CancellationToken.None).Result;
					
					await api.Disconnect().ConfigureAwait(false);

					Log.CloseAndFlush();
					
					return result == CommandResult.Success ? 0 : 1;
				}
				
				NLog.Default.Error($"Unexpected command type: {programArgs.ApiCommand.InstantiatedCommand.GetType()}");
			}

			return -1;

		}

	}
}