using System;
using System.Threading;
using System.Threading.Tasks;
using Neuralia.Blockchains.Tools;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Neuralium.Cli.Classes.Runtime {
	public interface ICliService : IHostedService, IDisposable2 {
	}

	public class CliService : ICliService {

		protected readonly IApplicationLifetime ApplicationLifetime;

		protected readonly ICliApp cliApp;

		public CliService(IApplicationLifetime ApplicationLifetime, ICliApp cliApp) {

			this.ApplicationLifetime = ApplicationLifetime;
			this.cliApp = cliApp;
		}

		public Task StartAsync(CancellationToken cancellationNeuralium) {

			Log.Information("Daemon is starting....");

			this.ApplicationLifetime.ApplicationStarted.Register(this.OnStarted);
			this.ApplicationLifetime.ApplicationStopping.Register(this.OnStopping);
			this.ApplicationLifetime.ApplicationStopped.Register(this.OnStopped);

			this.cliApp.Start();

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationNeuralium) {

			Log.Information("Daemon shutdown in progress...");

			this.cliApp.Stop();
			this.cliApp.WaitStop(TimeSpan.FromSeconds(10));

			this.cliApp.Dispose();

			return Task.CompletedTask;
		}

		protected virtual void OnStarted() {
			Log.Information("Daemon is successfully started.");

			// Post-startup code goes here
		}

		protected virtual void OnStopping() {
			Log.Information("Daemon shutdown requested.");
		}

		protected virtual void OnStopped() {
			Log.Information("Daemon successfully stopped");
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
					Log.Error(ex, "failed to dispose of Neuralium service");
				} finally {
					this.IsDisposed = true;
				}
			}
		}

		~CliService() {
			this.Dispose(false);
		}

	#endregion

	}
}