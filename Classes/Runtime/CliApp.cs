using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Neuralia.NClap.Metadata;
using Neuralia.NClap.Repl;
using Neuralia.Blockchains.Tools.Locking;
using Neuralia.Blockchains.Tools.Threading;
using Neuralium.Cli.Classes.API;
using Neuralium.Cli.Classes.Runtime.Commands;

namespace Neuralium.Cli.Classes.Runtime {
	public interface ICliApp : ILoopThread<ICliApp> {
	}

	public class CliApp<API, API_METHODS> : LoopThread<ICliApp>, ICliApp
		where API : NeuraliumApi<API_METHODS>, new()
		where API_METHODS : IApiMethods {

		private readonly API api;
		private readonly Loop loop;

		protected readonly IHostApplicationLifetime applicationLifetime;

		protected readonly AppSettings appSettings;

		protected readonly OptionsBase CmdModeratorInteractiveOptions;
		protected readonly IServiceProvider serviceProvider;

		private bool processing;
		private bool quitting;

		public CliApp(IServiceProvider serviceProvider, IHostApplicationLifetime applicationLifetime, IOptions<AppSettings> appSettings, OptionsBase interactiveOptions) {

			this.appSettings = appSettings.Value;
			this.CmdModeratorInteractiveOptions = interactiveOptions;
			this.applicationLifetime = applicationLifetime;
			this.serviceProvider = serviceProvider;

			this.api = new API();
			this.api.Init(this.appSettings, interactiveOptions, NeuraliumApi.UseModes.SendReceive);
			
			this.loop = new Loop(typeof(ApiCommands));
		}

		protected override async Task Initialize(LockContext lockContext) {
			await base.Initialize(lockContext).ConfigureAwait(false);

			try {
				await this.api.Connect().ConfigureAwait(false);
			} catch(Exception ex) {
				Console.WriteLine($"Failed to run daemon: {ex.Message}, will now shutdown....");
				Shutdown();
			}
		}

		protected override Task ProcessLoop(LockContext lockContext) {
			try
			{
				var result = loop.ExecuteOnce(c =>
				{
					if (c is CommandGroup<ApiCommands> group)
					{
						if (group.InstantiatedCommand is CommandBase<API, API_METHODS> command)
						{
							command.Arguments = this.CmdModeratorInteractiveOptions;
							command.Api = this.api;
						}
						
					}
				});
				
				if(result == CommandResult.Terminate)
					this.Shutdown();

			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				this.Shutdown();
			}
			
			
			return Task.CompletedTask;
		}

		protected void Shutdown() {
			this.quitting = true;
			this.applicationLifetime.StopApplication();
		}

		protected override async Task DisposeAllAsync() {

			try {
				await this.api.Disconnect().ConfigureAwait(false);

			} catch(Exception ex) {
				Console.WriteLine(ex);
			}
		}
	}
}