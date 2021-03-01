using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Neuralia.Blockchains.Core.Logging;
using Neuralium.Cli.Classes.API;
using Neuralium.Cli.Classes.Runtime;
using Serilog;

namespace Neuralium.Cli.Classes.SignalR {

	public class SignalrClient {
		private readonly HubConnection connection;
		private readonly string user;
		public SignalrClient(AppSettings appSettings, OptionsBase options) {

			string host = appSettings.Host;
			int port = appSettings.RpcPort;
			user = appSettings.User;
			if(!string.IsNullOrWhiteSpace(options.Host)) {
				host = options.Host;
			}

			if(options.Port.HasValue) {
				port = options.Port.Value;
			}

			if (!string.IsNullOrWhiteSpace(options.User)){
				user = options.User;
			}

			this.connection = new HubConnectionBuilder().WithUrl(new UriBuilder(appSettings.UseTls ? "https" : "http", host, port, "signal").ToString(), urlOptions =>
				{
					if(!string.IsNullOrWhiteSpace(user))
						urlOptions.AccessTokenProvider = () => Task.FromResult(user);
				}).WithAutomaticReconnect().AddJsonProtocol(jsonOptions => 
			{
				jsonOptions.PayloadSerializerOptions.WriteIndented = false;
			}).Build();

			this.connection.Closed += async error => {
				await Task.Delay(new Random().Next(0, 5) * 1000).ConfigureAwait(false);
				await this.connection.StartAsync().ConfigureAwait(false);
			};

		}

		public SignalrClient(AppSettings appSettings, OptionsBase options, IApiEvents eventHandler) : this(appSettings, options) {

			// make sure we can receive events
			this.RegisterEvents(eventHandler);
		}

		public string User => this.user;

		public bool IsConnected()
		{
			return connection.State == HubConnectionState.Connected;
		}
		public async Task Connect() {
			try {
				await this.connection.StartAsync().ConfigureAwait(false);
				NLog.Default.Debug("Connected to server");
			} catch(Exception ex) {
				Log.Logger.Error("failed to connect to server", ex);
			}
		}

		public async Task Disconnect() {

			try {
				await this.connection.StopAsync().ConfigureAwait(false);
				NLog.Default.Debug("Disconnected from server");
			} catch(Exception ex) {
				Log.Logger.Error("failed to disconnect to server", ex);
			}
		}

		public async Task<object> InvokeMethod(string operation, IEnumerable<object> parameters) {

			return this.InvokeMethod<object>(operation, parameters);
		}

		public async Task<T> InvokeMethod<T>(string operation, IEnumerable<object> parameters) {

			try {
				//careful: object will return JsonElement
				return (T) await this.connection.InvokeCoreAsync(operation, typeof(T), parameters.ToArray()).ConfigureAwait(false);

			} catch(Exception ex) {
				Log.Logger.Error($"failed to invoke method {operation}", ex);

				throw;
			}
		}

	#region events

		private void RegisterEvents(IApiEvents eventHandler) {
			
			foreach (var method in typeof(IApiEvents).GetMethods())
			{
				try
				{
					this.connection.On(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray(),
						(parameters, instance) => Task.FromResult(method.Invoke(instance, parameters)), eventHandler);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			}
			
		}

	#endregion

	}
}