using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Neuralia.Blockchains.Core.Logging;
using Neuralium.Cli.Classes.Runtime;
using Neuralium.Cli.Classes.SignalR;
using Newtonsoft.Json;
using Nito.AsyncEx.Synchronous;
using RestSharp.Extensions;
using Serilog;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Neuralium.Cli.Classes.API {

	public static class NeuraliumApi {
		public enum UseModes {
			SendOnly,
			SendReceive
		}
	}

	public class NeuraliumApi<API_METHODS> : IApiMethods, IApiEvents
		where API_METHODS : IApiMethods {

		/// <summary>
		///     the Neuralium constant chain Id
		/// </summary>
		private const ushort chainType = 1001;

		private readonly Dictionary<int, Task> longRunningTasks = new Dictionary<int, Task>();
		private readonly Dictionary<int, Action> longRunningTasksCallbacks = new Dictionary<int, Action>();
		private readonly Dictionary<int, string> longRunningCalls = new Dictionary<int, string>();
		
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

		public void RegisterLongRunningTaskCallback(int correlationId, Action callback)
		{
			if (useMode == NeuraliumApi.UseModes.SendOnly)
			{ 
				NLog.Default.Error($"{nameof(RegisterLongRunningTaskCallback)}: you're connected in SendOnly mode, callbacks are useless in this mode, not registering the callback.");
				return;
			}
				
			if(longRunningTasksCallbacks.ContainsKey(correlationId))
				NLog.Default.Warning($"{nameof(RegisterLongRunningTaskCallback)}: this correlation id already has a callback, replacing it...");

			longRunningTasksCallbacks[correlationId] = callback;

		}

		public async Task<bool> WaitForLongRunningTask(uint correlationId, double timeout = Double.MaxValue, double timeStep = 0.25)
		{
			long creationComplete = 0;
			RegisterLongRunningTaskCallback(unchecked((int)correlationId)
				, () => Interlocked.Increment(ref creationComplete));

			
			return await Task.Run(() =>
			{
				double timeElapsed = 0;
				while (Interlocked.Read(ref creationComplete) == 0)
				{
					Thread.Sleep(Convert.ToInt32(timeStep * 1000));
					timeElapsed += timeStep;
					if (timeElapsed > timeout)
					{
						NLog.Default.Information($"Timeout of {timeout} reached waiting for correlation id {correlationId}, aborting...");
						longRunningTasksCallbacks.Remove(unchecked((int)correlationId));
						return false;
					}
				}

				return true;

			}).ConfigureAwait(false);
		}

		public async Task Connect()
		{
			await this.signalrClient.Connect().ConfigureAwait(false);
		}

		public async Task Disconnect()
		{
			await this.signalrClient.Disconnect().ConfigureAwait(false);
		}

		public async Task<string> InvokeMethod(IQueryJson parameters, double timeoutForLongOperation = 0) {

			MethodInfo methodInfo = typeof(API_METHODS).GetMethod(parameters.Operation, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

			if(methodInfo == null) {
				throw new ArgumentException("Operation was not found");
			}

			ParameterInfo[] parameterInfos = methodInfo.GetParameters();

			if(parameters.ParameterCount != parameterInfos.Length) {
				// we will jus try nulls
				NLog.Default.Warning("Parameter count is different. Nulls will be assigned to missing parameters");

				//throw new ArgumentException("Invalid parameter count");
			}

			object[] methodParameters = new object[parameterInfos.Length];

			for(int i = 0; i < methodParameters.Length; i++) {
				if(parameters.ParameterCount > i) {

					void GetParamenter(string value) {
						if(!DeserializeParameter(value, parameterInfos[i].ParameterType, out methodParameters[i])) {
							methodParameters[i] = JsonConvert.DeserializeObject(value, parameterInfos[i].ParameterType);
						}
					}

					if(parameters is QueryJsonNamed query) {
						QueryJsonNamed.NamedOperationParameters paramValue = query.Parameters.SingleOrDefault(e => string.Equals(e.Name, parameterInfos[i].Name, StringComparison.CurrentCultureIgnoreCase));

						if(paramValue != null) {

							GetParamenter(paramValue.Value);
						}
					} else if(parameters is QueryJsonIndexed indexed) {

						QueryJsonIndexed.IndexedOperationParameters paramValue = indexed.Parameters.SingleOrDefault(e => e.HasName && string.Equals(e.Name, parameterInfos[i].Name, StringComparison.CurrentCultureIgnoreCase));

						if(paramValue != null) {
							GetParamenter(paramValue.Element);
						} else {
							// get by index
							List<string> list = indexed.FormattedParameters.ToList();
							GetParamenter(list[i]);
						}
					}
				}
			}

			object result = null;
			NLog.Default.Information($"invoking method {parameters.Operation}");

			Task task = (Task) methodInfo.Invoke(this, methodParameters);

			await task.ConfigureAwait(false);

			NLog.Default.Information($"method invoked and returned {parameters.Operation}");

			Type taskType = task.GetType();
			
			if(taskType.IsGenericType)
			{
				result = taskType.GetProperty("Result")?.GetValue(task);

				if (timeoutForLongOperation > 0
				    && result is int correlationId
				    && this.longRunningCalls.TryGetValue(correlationId, out var operation)
				    && parameters.Operation == operation)
				{
					if (await this.WaitForLongRunningTask(unchecked((uint)correlationId), timeoutForLongOperation).ConfigureAwait(false))
						result = true; //else result still contains correlationId
				}
				
			}

			return result == null ? "" : JsonSerializer.Serialize(result, new JsonSerializerOptions {WriteIndented = true});
		}

		private static bool DeserializeParameter(string serialized, Type type, out object result) {

			result = null;

			try {
				if(type == typeof(Guid)) {
					if(Guid.TryParse(serialized, out Guid guid)) {
						result = guid;
					}
				} else if(type == typeof(string)) {
					result = serialized;
				} else if(type == typeof(bool)) {
					if(bool.TryParse(serialized, out bool guid)) {
						result = guid;
					}
				}
			} catch(Exception ex) {
				NLog.Default.Error(ex, $"Failed to serialize parameter value '{serialized}'");
			}

			return result != null;
		}

		private async Task<uint> InvokeLongRunningMethod(string operation, IEnumerable<object> parameters) {
			if(this.useMode == NeuraliumApi.UseModes.SendOnly) {
				NLog.Default.Warning("We are in send only mode. Correlation events will not be captured");
			}

			NLog.Default.Information($"invoking long running method {operation}");
			int correlationId = await this.signalrClient.InvokeMethod<int>(operation, parameters).ConfigureAwait(false);

			NLog.Default.Information($"Long running method invoked and returned {operation} with correlation Value {correlationId}");

			this.longRunningTasks.Add(correlationId, null);
			this.longRunningCalls.Add(correlationId, operation);

			return unchecked((uint) correlationId);
		}

		protected string GetCallingMethodName([CallerMemberName] string caller = null) {
			return caller;
		}

	#region events

		public void RequestCopyWallet(string path){
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {path}");
		}

		public void EnterWalletPassphrase(int correlationId, string accountCode, string passphrase) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {accountCode} {passphrase.HasValue()}");
			this.CompleteLongRunningEvent(unchecked((uint)correlationId), new[] {accountCode, passphrase}).WaitAndUnwrapException();
		}

		public void EnterWalletKeyPassphrase(string accountCode, string path) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {accountCode} {path}");
		}

		public void WalletTotalUpdated(int correlationId, string accountCode, double total) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {accountCode} {total}");
		}

		/// <summary>
		///     a client triggered long running even has completed. take the return value and clear the cache
		/// </summary>
		/// <param name="correlationId"></param>
		/// <param name="result"></param>
		public void ReturnLongRunningEvent(int correlationId, bool result, string error) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {result} {error}");
		}

		public void LongRunningStatusUpdate(int correlationId, ushort eventId, byte eventType, string message) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {correlationId} {eventId} {eventType} {message}");
		}

		public void AccountPublicationCompleted(int correlationId, string accountCode, bool result, long accountSequenceId, byte accountType) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {accountCode} {result} {accountSequenceId} {accountType}");
		}

		public void EnterWalletPassphrase(int correlationId, int keyCorrelationCode, int attempt) {

			NLog.Default.Information($"Event {this.GetCallingMethodName()} {correlationId} {keyCorrelationCode} {attempt}");
			this.EnterWalletPassphrase(unchecked((uint)correlationId), unchecked((uint)keyCorrelationCode), Console.ReadLine()).WaitAndUnwrapException();
		}

		public void EnterKeysPassphrase(int correlationId, int keyCorrelationCode, string accountCode, string keyname, int attempt) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {correlationId} {keyCorrelationCode} {accountCode} {keyname} {attempt}");
			this.EnterKeyPassphrase(unchecked((uint)correlationId), unchecked((uint)keyCorrelationCode), Console.ReadLine()).WaitAndUnwrapException();
		}

		public void walletCreationStarted(int correlationId)
		{
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {correlationId}");
		}
		
		public void WalletCreationEnded(int correlationId)
		{
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {correlationId}");
		}

		public void CopyWalletKeyFile(int correlationId, int keyCorrelationCode, string accountCode, string keyname, int attempt) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {correlationId} {keyCorrelationCode} {accountCode} {keyname} {attempt}");
			throw new NotImplementedException();
		}

		public void ReturnClientLongRunningEvent(int correlationId, int result, string error) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {correlationId} {result} {error}");

			if(this.longRunningTasks.ContainsKey(correlationId)) {
				this.longRunningTasks.Remove(correlationId);
			}
			
			if(longRunningTasksCallbacks.ContainsKey(correlationId)) {
				longRunningTasksCallbacks[correlationId].Invoke();
				longRunningTasksCallbacks.Remove(correlationId);
			}
		}

	#endregion

	#region Methods

		public async Task<bool> CompleteLongRunningEvent(uint correlationId, object data) {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new[] {correlationId, data}).ConfigureAwait(false);
		}

		public async Task<bool> RenewLongRunningEvent(uint correlationId) {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {correlationId}).ConfigureAwait(false);
		}

		public async Task Test()
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[0]).ConfigureAwait(false);
		}

		public async Task<bool> Ping() {
			string result = await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new string[0]).ConfigureAwait(false);

			return result == "pong";
		}

		public async Task<bool> Shutdown() {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[0]).ConfigureAwait(false);
		}

		public async Task<object> QueryBlockChainInfo() {
			return this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType});
		}

		public async Task<object> CanPublishAccount(string accountCode) {
			return await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}
		
		public async Task<uint> PublishAccount(string accountCode) {
			return await InvokeLongRunningMethod(GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		public async Task StartMining(string delegateAccountId, int tier = 0)
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType, delegateAccountId, tier}).ConfigureAwait(false);
		}

		public async Task StopMining()
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> IsMiningEnabled() {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<object> QueryElectionContext(long blockId) {
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<string> QueryBlock(long blockId) {
			return await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<byte[]> QueryCompressedBlock(long blockId) {
			return await this.signalrClient.InvokeMethod<byte[]>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<uint> SendNeuraliums(string targetAccountId, decimal amount, decimal tip, string note) {
			return await InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {targetAccountId, amount, tip, note}).ConfigureAwait(false);
		}

		public async Task EnterWalletPassphrase(uint correlationId, uint keyCorrelationCode, string passphrase)
		{
			passphrase = AskPassphrase(passphrase);
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {unchecked((uint)correlationId), chainType, keyCorrelationCode, passphrase}).ConfigureAwait(false);
		}

		public async Task EnterKeyPassphrase(uint correlationId, uint keyCorrelationCode, string passphrase)
		{
			passphrase = AskPassphrase(passphrase);
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {unchecked((uint)correlationId), chainType, keyCorrelationCode, passphrase}).ConfigureAwait(false);
		}

		public async Task WalletKeyFileCopied(uint correlationId, uint keyCorrelationCode)
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {unchecked((uint)correlationId), chainType, keyCorrelationCode}).ConfigureAwait(false);
		}

		public async Task<object> QuerySupportedChains() {
			return await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[0]).ConfigureAwait(false);

		}

	#endregion

	#region common chain methods

		public async Task<long> QueryBlockHeight() {
			return await this.signalrClient.InvokeMethod<long>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<object> QueryChainStatus() {
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> IsWalletLoaded() {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<uint> LoadWallet(string passphrase)
		{
			passphrase = AskPassphrase(passphrase);
			return await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {chainType, passphrase}).ConfigureAwait(false);
		}

		private static string AskPassphrase(string passphrase)
		{
			if (passphrase == null)
			{
				Console.WriteLine("Please enter your password, the press 'Enter':");
				passphrase = Console.ReadLine();
			}

			return passphrase;
		}

		public async Task<bool> WalletExists() {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<uint> CreateNewWallet(string accountName, int accountType, bool encryptWallet, bool encryptKey, bool encryptKeysIndividually, ImmutableDictionary<string, string> passphrases, bool publishAccount) {
			return unchecked((uint) await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {chainType, accountName, accountType, encryptWallet, encryptKey, encryptKeysIndividually, passphrases, publishAccount}).ConfigureAwait(false));
		}

		public async Task<List<object>> QueryWalletTransactionHistory(string accountCode) {
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		public async Task<List<object>> QueryWalletAccounts() {
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<uint> PresentAccountPublicly() {
			return await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<List<object>> QueryBlockBinaryTransactions(long blockId) {
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

	#endregion

	#region Neuralium chain methods

		public async Task<object> QueryAccountTotalNeuraliums(string accountCode) {
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {accountCode}).ConfigureAwait(false);
		}

		public async Task<uint> SendNeuraliums(long recipientAccountId, double amount, double fees) {
			return await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {recipientAccountId, amount, fees}).ConfigureAwait(false);
		}

	#endregion

	}

}