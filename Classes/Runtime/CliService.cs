using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Neuralia.Blockchains.Core.Logging;
using Neuralia.Blockchains.Tools;
using Serilog;

namespace Neuralium.Cli.Classes.Runtime {
	public interface ICliService : IHostedService, IDisposableExtended {
	}

	public class CliService : ICliService {

		protected readonly IHostApplicationLifetime ApplicationLifetime;

		protected readonly ICliApp cliApp;

		public CliService(IHostApplicationLifetime ApplicationLifetime, ICliApp cliApp) {

			this.ApplicationLifetime = ApplicationLifetime;
			this.cliApp = cliApp;
		}

		public Task StartAsync(CancellationToken cancellationNeuralium) {

			NLog.Default.Information("Daemon is starting....");

			this.ApplicationLifetime.ApplicationStarted.Register(this.OnStarted);
			this.ApplicationLifetime.ApplicationStopping.Register(this.OnStopping);
			this.ApplicationLifetime.ApplicationStopped.Register(this.OnStopped);

			return this.cliApp.Start();

		}

		public async Task StopAsync(CancellationToken cancellationNeuralium) {

			NLog.Default.Information("Daemon shutdown in progress...");

			await this.cliApp.Stop().ConfigureAwait(false);
			this.cliApp.WaitStop(TimeSpan.FromSeconds(10));

			this.cliApp.Dispose();
		}

		protected virtual void OnStarted() {
			NLog.Default.Information("Daemon is successfully started.");

			// Post-startup code goes here
		}

		protected virtual void OnStopping() {
			NLog.Default.Information("Daemon shutdown requested.");
		}

		protected virtual void OnStopped() {
			NLog.Default.Information("Daemon successfully stopped");
		}

	#region Dispose

		public bool IsDisposed { get; private set; }

		public void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {

			if(disposing && !this.IsDisposed) {

				try {
					try {

					} catch(Exception ex) {
						Console.WriteLine(ex);
					}

				} catch(Exception ex) {
					NLog.Default.Error(ex, "failed to dispose of Neuralium service");
				}
			}

			this.IsDisposed = true;
		}

		~CliService() {
			this.Dispose(false);
		}

	#endregion

	}
}