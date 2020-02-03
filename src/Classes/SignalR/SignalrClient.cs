using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Neuralium.Cli.Classes.API;
using Neuralium.Cli.Classes.Runtime;
using Serilog;

namespace Neuralium.Cli.Classes.SignalR {
	
	public class SignalrClient {
		private readonly HubConnection connection;

		public SignalrClient(AppSettings appSettings, OptionsBase options) {

			string host = appSettings.Host;
			int port = appSettings.RpcPort;
			
			if(!string.IsNullOrWhiteSpace(options.Host)) {
				host = options.Host;
			}

			if(options.Port.HasValue) {
				port = options.Port.Value;
			}

			this.connection = new HubConnectionBuilder().WithUrl(new UriBuilder(appSettings.UseTls ? "https" : "http", host, port, "signal").ToString()).WithAutomaticReconnect().AddJsonProtocol(options =>
			{
				options.PayloadSerializerOptions.WriteIndented = false;
			}).Build();

			this.connection.Closed += async error => {
				await Task.Delay(new Random().Next(0, 5) * 1000);
				await this.connection.StartAsync();
			};

		}

		public SignalrClient(AppSettings appSettings, OptionsBase options, IApiEvents eventHandler) : this(appSettings, options) {

			// make sure we can receive events
			this.RegisterEvents(eventHandler);
		}

		public async Task Connect() {
			try {
				await this.connection.StartAsync();
				Log.Debug("Connected to server");
			} catch(Exception ex) {
				Log.Logger.Error("failed to connect to server", ex);
			}
		}

		public async Task Disconnect() {

			try {
				await this.connection.StopAsync();
				Log.Debug("Disconnected from server");
			} catch(Exception ex) {
				Log.Logger.Error("failed to disconnect to server", ex);
			}
		}

		public Task<object> InvokeMethod(string operation, IEnumerable<object> parameters) {

			return this.InvokeMethod<object>(operation, parameters);
		}

		public async Task<T> InvokeMethod<T>(string operation, IEnumerable<object> parameters) {

			try {
				//careful: object will return JsonElement
				return (T) await this.connection.InvokeCoreAsync(operation, typeof(T), parameters.ToArray());



			} catch(Exception ex) {
				Log.Logger.Error($"failed to invoke method {operation}", ex);

				throw;
			}
		}

	#region events

		private void RegisterEvents(IApiEvents eventHandler) {

			//TODO: use reflection to set events

			this.connection.On<int, int, int>(nameof(eventHandler.EnterWalletPassphrase), eventHandler.EnterWalletPassphrase);
			this.connection.On<int, int, Guid, string, int>(nameof(eventHandler.EnterKeysPassphrase), eventHandler.EnterKeysPassphrase);
			this.connection.On<int, int, Guid, string, int>(nameof(eventHandler.CopyWalletKeyFile), eventHandler.CopyWalletKeyFile);

			this.connection.On<int, int, string>(nameof(eventHandler.ReturnClientLongRunningEvent), eventHandler.ReturnClientLongRunningEvent);

			this.connection.On<string>(nameof(eventHandler.RequestCopyWallet), eventHandler.RequestCopyWallet);
			this.connection.On<int, Guid, string>(nameof(eventHandler.EnterWalletPassphrase), eventHandler.EnterWalletPassphrase);
			this.connection.On<Guid, string>(nameof(eventHandler.EnterWalletKeyPassphrase), eventHandler.EnterWalletKeyPassphrase);

			this.connection.On<int, ushort, byte, string>(nameof(eventHandler.LongRunningStatusUpdate), eventHandler.LongRunningStatusUpdate);
			this.connection.On<int, Guid, bool, long, byte>(nameof(eventHandler.AccountPublicationCompleted), eventHandler.AccountPublicationCompleted);
			this.connection.On<int, Guid, double>(nameof(eventHandler.WalletTotalUpdated), eventHandler.WalletTotalUpdated);
		}

	#endregion

	}
}