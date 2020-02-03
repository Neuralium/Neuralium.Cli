using System;
using System.Linq;
using System.Threading;
using Neuralia.Blockchains.Tools.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Neuralium.Cli.Classes.API;
using Serilog;

namespace Neuralium.Cli.Classes.Runtime {
	public interface ICliApp : ILoopThread<ICliApp> {
	}

	public class CliApp<API, API_METHODS> : LoopThread<ICliApp>, ICliApp
		where API : NeuraliumApi<API_METHODS>, new()
		where API_METHODS : IApiMethods {

		private readonly API api;

		protected readonly IHostApplicationLifetime applicationLifetime;

		protected readonly AppSettings appSettings;

		protected readonly InteractiveOptions CmdModeratorInteractiveOptions;
		protected readonly IServiceProvider serviceProvider;

		private bool processing;
		private bool quitting;

		public CliApp(IServiceProvider serviceProvider, IHostApplicationLifetime applicationLifetime, IOptions<AppSettings> appSettings, InteractiveOptions moderatorInteractiveOptions) {

			this.appSettings = appSettings.Value;
			this.CmdModeratorInteractiveOptions = moderatorInteractiveOptions;
			this.applicationLifetime = applicationLifetime;
			this.serviceProvider = serviceProvider;

			this.api = new API();
			this.api.Init(this.appSettings, moderatorInteractiveOptions, NeuraliumApi.UseModes.SendReceive);
		}

		protected override async void Initialize() {
			base.Initialize();

			try {
				await this.api.Connect();

			} catch(Exception ex) {
				throw new ApplicationException("Failed to run daemon", ex);
			}
		}

		protected override void ProcessLoop() {

		}

		protected void Shutdown() {
			this.quitting = true;
			this.applicationLifetime.StopApplication();
		}

		protected override async void DisposeAll() {
			
			try {
				await this.api.Disconnect();

			} catch(Exception ex) {
				Console.WriteLine(ex);
			}
		}
	}
}