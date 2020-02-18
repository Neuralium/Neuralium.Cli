using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Neuralium.Cli.Classes.Runtime;
using Neuralium.Cli.Classes.SignalR;
using Serilog;

namespace Neuralium.Cli.Classes.API {

	public static class NeuraliumApi {
		public enum UseModes {
			SendOnly,
			SendReceive
		}
	}

	public class NeuraliumApi<API_METHODS> : IApiMethods, IApiEvents
		where API_METHODS : IApiMethods {

		private readonly Dictionary<int, Task> longRunningTasks = new Dictionary<int, Task>();

		/// <summary>
		/// the Neuralium constant chain Id
		/// </summary>
		private const ushort chainType = 1001;
		
		protected SignalrClient signalrClient;
		protected NeuraliumApi.UseModes useMode;

		public void Init(AppSettings appSettings, OptionsBase options, NeuraliumApi.UseModes useMode) {
			this.useMode = useMode;

			if(useMode == NeuraliumApi.UseModes.SendReceive) {
				this.signalrClient = new SignalrClient(appSettings, options, this);
			} else if(useMode == NeuraliumApi.UseModes.SendOnly) {
				this.signalrClient = new SignalrClient(appSettings, options);
			}
		}

		public async Task Connect() {
			await this.signalrClient.Connect();
		}

		public async Task Disconnect() {
			await this.signalrClient.Disconnect();
		}

		public async Task<string> InvokeMethod(IQueryJson parameters ) {

			MethodInfo methodInfo = typeof(API_METHODS).GetMethod(parameters.Operation, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

			if(methodInfo == null) {
				throw new ArgumentException("Operation was not found");
			}

			var parameterInfos = methodInfo.GetParameters();
			
			if(parameters.ParameterCount != parameterInfos.Length) {
				// we will jus try nulls
				Log.Warning("Parameter count is different. Nulls will be assigned to missing parameters");
				//throw new ArgumentException("Invalid parameter count");
			}

			var methodParameters = new object[parameterInfos.Length];

			for(int i = 0; i < methodParameters.Length; i++) {
				if(parameters.ParameterCount > i) {

					void GetParamenter(string value) {
						if(!DeserializeParameter(value, parameterInfos[i].ParameterType, out methodParameters[i])) {
							methodParameters[i] = Newtonsoft.Json.JsonConvert.DeserializeObject(value, parameterInfos[i].ParameterType);
						}
					}
					if(parameters is QueryJsonNamed query) {
						var paramValue = query.Parameters.SingleOrDefault(e => string.Equals(e.Name, parameterInfos[i].Name, StringComparison.CurrentCultureIgnoreCase));

						if(paramValue != null) {

							GetParamenter(paramValue.Value);
						}
					}
					else if(parameters is QueryJsonIndexed indexed) {

						var paramValue = indexed.Parameters.SingleOrDefault(e => e.HasName && string.Equals(e.Name, parameterInfos[i].Name, StringComparison.CurrentCultureIgnoreCase));
						
						if(paramValue != null) {
							GetParamenter(paramValue.Element);
						} else {
							// get by index
							var list = indexed.FormattedParameters.ToList();
							GetParamenter(list[i]);
						}
					}
				}
			}

			object result = null;
			Log.Information($"invoking method {parameters.Operation}");

			Task task =  (Task) methodInfo.Invoke(this, methodParameters);

			await task;
			
			Log.Information($"method invoked and returned {parameters.Operation}");

			Type taskType = task.GetType();

			if(task.GetType().IsGenericType) {
				result = taskType.GetProperty("Result")?.GetValue(task);
			} 
			
			return result == null ? "" : JsonSerializer.Serialize(result, new JsonSerializerOptions(){WriteIndented = true});
		}

		private static bool DeserializeParameter(string serialized, Type type, out object result) {

			result = null;

			try {
				if(type == typeof(Guid)) {
					if(Guid.TryParse(serialized, out Guid guid)) {
						result = guid;
					}
				}
				else if(type == typeof(string)) {
					result = serialized;
				}
				else if(type == typeof(bool)) {
					if(bool.TryParse(serialized, out bool guid)) {
						result = guid;
					}
				}
			} catch(Exception ex) {
				Log.Error(ex, $"Failed to serialize parameter value '{serialized}'");
			}

			return result != null;
		}
		
		private async Task<int> InvokeLongRunningMethod(string operation, IEnumerable<object> parameters) {
			if(this.useMode == NeuraliumApi.UseModes.SendOnly) {
				Log.Warning("We are in send only mode. Correlation events will not be captured");
			}

			Log.Information($"invoking long running method {operation}");
			int correlationId = await this.signalrClient.InvokeMethod<int>(operation, parameters);

			Log.Information($"Long running method invoked and returned {operation} with correlation Value {correlationId}");

			this.longRunningTasks.Add(correlationId, null);

			return correlationId;
		}

		protected string GetCallingMethodName([CallerMemberName] string caller = null) {
			return caller;
		}

	#region events

		public void RequestCopyWallet(string path) {
			Log.Information("Server is requesting that the wallet be loaded");
		}

		public async void EnterWalletPassphrase(int correlationId, Guid accountID, string path) {
			Log.Information("Server is requesting that wallet passphrase be provided");

			Thread.Sleep(1000);
			await this.CompleteLongRunningEvent(correlationId, new[] {"voila!"});
		}

		public void EnterWalletKeyPassphrase(Guid accountID, string path) {
			Log.Information("Server is requesting that wallet passphrase be provided");
		}

		public void WalletTotalUpdated(int correlationId, Guid accountUuid, double total) {
			Log.Information($"Wallet total was updated to {total}");
		}

		/// <summary>
		///     a client triggered long running even has completed. take the return value and clear the cache
		/// </summary>
		/// <param name="correlationId"></param>
		/// <param name="result"></param>
		public void ReturnLongRunningEvent(int correlationId, bool result, string error) {
			Log.Information("Long running event returned");

			if(this.longRunningTasks.ContainsKey(correlationId)) {
				this.longRunningTasks.Remove(correlationId);
			}
		}

		public void LongRunningStatusUpdate(int correlationId, ushort eventId, byte eventType, string message) {
			Log.Information($"Long running event raised. message: {message}");
		}

		public void AccountPublicationCompleted(int correlationId, Guid accountUuid, bool result, long accountSequenceId, byte accountType) {
			Log.Information($"Long running account publication completed. accountId: {accountSequenceId}");
		}

		public async void EnterWalletPassphrase(int correlationId, int keyCorrelationCode, int attempt) {

			Log.Information("Enter wallet passphrase:");

			Thread.Sleep(100);
			string passphrase = "qwerty";

			Console.WriteLine("sent");
			await this.EnterWalletPassphrase(correlationId, keyCorrelationCode, passphrase);

		}

		public async void EnterKeysPassphrase(int correlationId, int keyCorrelationCode, Guid accountID, string keyname, int attempt) {
			Log.Information($"Enter wallet passphrase for key '{keyname}' and account ' {accountID}':");

			Thread.Sleep(100);
			string passphrase = "";

			if(attempt == 1) {
				passphrase = "toto";
			} else if(attempt == 2) {
				passphrase = "qwerty";
			} else {
				passphrase = "qwerty2";
			}

			Console.WriteLine("sent");
			await this.EnterKeyPassphrase(correlationId, keyCorrelationCode, passphrase);
		}

		public async void CopyWalletKeyFile(int correlationId, int keyCorrelationCode, Guid accountID, string keyname, int attempt) {
			Log.Information($"Copy file for wallet key '{keyname}' and account ' {accountID}':");

			Console.ReadKey();

			Console.WriteLine("sent");
			await this.WalletKeyFileCopied(correlationId, keyCorrelationCode);
		}

		public void ReturnClientLongRunningEvent(int correlationId, int result, string error) {
			Log.Information($"Long running client event completed. accountId: {result}");
		}

	#endregion

	#region Methods

		public Task<bool> CompleteLongRunningEvent(int correlationId, object data) {
			return this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new[] {correlationId, data});
		}

		public Task<bool> RenewLongRunningEvent(int correlationId) {
			return this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {correlationId});
		}

		public Task Test() {
			return this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new string[0]);

		}

		public async Task<bool> Ping() {
			string result = await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new string[0]);

			return result.ToString() == "pong";
		}

		public async Task<bool> Shutdown() {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[0]);
		}

		public async Task<object> QueryBlockChainInfo() {
			return await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[]{chainType});
		}

		public async Task<int> PublishAccount(string accountUuid) {
			return await this.signalrClient.InvokeMethod<int>(this.GetCallingMethodName(), new object[]{chainType, accountUuid});
		}

		public async Task StartMining(string delegateAccountId) {
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType, delegateAccountId});
		}

		public async Task StopMining() {
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[]{chainType});
		}

		public async Task<bool> IsMiningEnabled() {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[]{chainType});
		}
		
		public async Task<object> QueryElectionContext(long blockId) {
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType, blockId});
		}
		
		public async Task<string> QueryBlock(long blockId) {
			return await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new object[] {chainType, blockId});
		}

		public async Task<byte[]> QueryCompressedBlock(long blockId) {
			return await this.signalrClient.InvokeMethod<byte[]>(this.GetCallingMethodName(), new object[] {chainType, blockId});
		}

		public async Task<int> SendNeuraliums(string targetAccountId, decimal amount, decimal tip, string note) {
			return await this.signalrClient.InvokeMethod<int>(this.GetCallingMethodName(), new object[] {targetAccountId, amount, tip, note});
		}

		public async Task EnterWalletPassphrase(int correlationId, int keyCorrelationCode, string passphrase) {
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {correlationId, chainType, keyCorrelationCode, passphrase});

		}

		public async Task EnterKeyPassphrase(int correlationId, int keyCorrelationCode, string passphrase) {
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {correlationId, chainType, keyCorrelationCode, passphrase});
		}

		public async Task WalletKeyFileCopied(int correlationId, int keyCorrelationCode) {
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {correlationId, chainType, keyCorrelationCode});
		}

		public Task<object> QuerySupportedChains() {
			return this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[0]);

		}

	#endregion

	#region common chain methods

		public Task<long> QueryBlockHeight() {
			return this.signalrClient.InvokeMethod<long>(this.GetCallingMethodName(), new object[] {chainType});
		}

		public Task<object> QueryChainStatus() {
			return this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType});
		}

		public Task<bool> IsWalletLoaded() {
			return this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType});
		}

		public async Task LoadWallet() {
			await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {chainType});
		}

		public Task<bool> WalletExists() {
			return this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType});
		}

		public async Task<int> CreateNewWallet(string accountName, bool encryptWallet, bool encryptKey, bool encryptKeysIndividually, Dictionary<int, string> passphrases, bool publishAccount) {
			return await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {chainType, accountName, encryptWallet, encryptKey, encryptKeysIndividually, passphrases, publishAccount});
		}

		public Task<List<object>> QueryWalletTransactionHistory(Guid accountUuid) {
			return this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType, accountUuid});
		}

		public Task<List<object>> QueryWalletAccounts() {
			return this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType});
		}

		public async Task PresentAccountPublicly() {
			await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {chainType});
		}

		public Task<List<object>> QueryBlockBinaryTransactions(long blockId) {
			return this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType, blockId});
		}

	#endregion

	#region Neuralium chain methods

		public Task<object> QueryAccountTotalNeuraliums(Guid accountUuid) {
			return this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] { accountUuid});
		}

		public async Task SendNeuraliums(long recipientAccountId, double amount, double fees) {
			await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {recipientAccountId, amount, fees});
		}

	#endregion

	}

}