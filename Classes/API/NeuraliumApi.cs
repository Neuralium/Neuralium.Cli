using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Neuralia.Blockchains.Core;
using Neuralia.Blockchains.Core.Extensions;
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
		private readonly Dictionary<int, Action<int, int, string>> longRunningTasksCallbacks = new Dictionary<int, Action<int, int, string>>();
		private readonly Dictionary<int, string> longRunningCalls = new Dictionary<int, string>();

		private readonly Dictionary<Type, LongRunningStatusBase> longRunningStatus =
			new Dictionary<Type, LongRunningStatusBase>();
		
		protected SignalrClient signalrClient;
		protected NeuraliumApi.UseModes useMode;

		private T GocLongRunningStatus<T>(bool resetFields = false) where T : LongRunningStatusBase
		{
			T value = null;
			
			if (longRunningStatus.TryGetValue(typeof(T), out var outValue))
				value = (T)outValue;
			else
				value = (T) (longRunningStatus[typeof(T)] = Activator.CreateInstance<T>());

			if(resetFields)
				value.ResetFields();
			
			return value;
		}
		
		public void Init(AppSettings appSettings, OptionsBase options, NeuraliumApi.UseModes useMode) {
			this.useMode = useMode;

			if(useMode == NeuraliumApi.UseModes.SendReceive) {
				this.signalrClient = new SignalrClient(appSettings, options, this);
			} else if(useMode == NeuraliumApi.UseModes.SendOnly) {
				this.signalrClient = new SignalrClient(appSettings, options);
			}
		}

		public bool IsConnected()
		{
			return this.signalrClient.IsConnected();
		}
		public void RegisterLongRunningTaskCallback(int correlationId, Action<int, int, string> callback)
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

		public async Task<bool> WaitForLongRunningTask(int correlationId, double timeout = Double.MaxValue)
		{
			long creationComplete = 0;
			RegisterLongRunningTaskCallback(correlationId
				, (cid, result, error) => Interlocked.Increment(ref creationComplete));

			
			return await Task.Run(() =>
			{
				double timeStep = 0.25;
				double timeElapsed = 0;
				while (Interlocked.Read(ref creationComplete) == 0)
				{
					Thread.Sleep(Convert.ToInt32(timeStep * 1000));
					timeElapsed += timeStep;
					if (timeElapsed > timeout)
					{
						NLog.Default.Information($"Timeout of {timeout} reached waiting for correlation id {correlationId}, aborting...");
						longRunningTasksCallbacks.Remove(correlationId);
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
					if (await this.WaitForLongRunningTask(correlationId, timeoutForLongOperation).ConfigureAwait(false))
						result = true; //else result still contains correlationId
				}
				
			}

			return result == null ? "" : JsonSerializer.Serialize(result, new JsonSerializerOptions {WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve});
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

		private async Task<int> InvokeLongRunningMethod(string operation, IEnumerable<object> parameters, Action<int, int, string>? callback = null) {
			if(this.useMode == NeuraliumApi.UseModes.SendOnly) {
				NLog.Default.Warning("We are in send only mode. Correlation events will not be captured");
			}

			NLog.Default.Information($"invoking long running method {operation}");
			int correlationId = await this.signalrClient.InvokeMethod<int>(operation, parameters).ConfigureAwait(false);

			NLog.Default.Information($"Long running method invoked and returned {operation} with correlation Value {correlationId}");

			this.longRunningTasks.Add(correlationId, null);
			
			if(callback != null)
				this.RegisterLongRunningTaskCallback(correlationId, callback);
			
			this.longRunningCalls.Add(correlationId, operation);

			return correlationId;
		}

		protected string GetCallingMethodName([CallerMemberName] string caller = null) {
			return caller;
		}

	#region events
		enum LogLevel
		{
			Information,
			Verbose,
			Error,
			Debug,
			Fatal,
			Warning
		}
			
		void PrintEvent(LogLevel level, string name, int correlationId, string extraMessage = "")
		{
			PrintEventMessage(level, name, extraMessage);
				
			NLog.Default.Debug( $"Correlation id: {correlationId}\n");
		}
		void PrintEvent(LogLevel level, string name, string extraMessage = "", string debugMessage = "")
		{
			PrintEventMessage(level, name, extraMessage);

			if (debugMessage.Length > 0)
				NLog.Default.Debug(debugMessage);
		}

		private static void PrintEventMessage(LogLevel level, string name, string extraMessage)
		{
			string message =
				(name != "" ? $"[{name}] " : "") +
				extraMessage;

			if (level == LogLevel.Information)
				Console.WriteLine(message);

			NLog.Default.GetType().GetMethod(level.ToString(), new Type[] {typeof(string)})
				.Invoke(NLog.Default, new object[] {message});
		}

		public void RequestCopyWallet(string path)
		{
			PrintEvent(LogLevel.Information, $"path: {path}");
		}

		public void LongRunningStatusUpdate(int correlationId, ushort eventId, byte eventType, ushort blockchainType, object message)
		{
			try
			{
				string name = BlockchainSystemEventTypes.Instance.GetNameFromUShort(eventId);
				
				PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), correlationId, $"Generic event id: {name}, event type {eventType}, message: {message}.");

				foreach (var (key, value) in longRunningStatus) //TODO, create a mapping of event id -> LongRunningTask so we dont loop over all handlers
				{
					value.ParseGenericEvent(correlationId, eventId, eventType, blockchainType, message);
				}

			}
			catch (Exception e)
			{
				NLog.Default.Error(e, $"event id: {eventId}, event type {eventType}, message: {message}." );
			}
		}

		public async void EnterWalletPassphrase(int correlationId, ushort chainType, int keyCorrelationId, int attempt)
		{
			PrintEvent(LogLevel.Information, "", correlationId, $"Enter your wallet passphrase (this is attempt number {attempt}):.");
			await this.EnterWalletPassphrase(correlationId, keyCorrelationId, null).ConfigureAwait(false);
		}


		public void EnterKeysPassphrase(int correlationId, ushort chainType, int keyCorrelationCode, string accountCode, string keyname, int attempt) {
			
			PrintEvent(LogLevel.Information, "", correlationId, $"Enter passphrase for key {keyname} of account {accountCode}, (this is attempt number {attempt}):");
			this.EnterKeyPassphrase(correlationId, keyCorrelationCode, Console.ReadLine()).WaitAndUnwrapException();
		}



		private class LongRunningStatusBase
		{
			public bool Ended = false;
			public bool Error = false;
			public string ErrorMessage = ""; 
			
			public void ResetFields()
			{
				var type = this.GetType();
				var blank = Activator.CreateInstance(type);
				foreach (var field in type.GetFields())
				{
					field.SetValue(this, field.GetValue(blank));
				}
			}

			public virtual void ReturnClientLongRunningEventCallback(int correlationId, int resultCode, string Error)
			{
				lock (this)
				{
					this.Error = resultCode != 0;
					this.ErrorMessage = Error;
					if(this.Error)
						Console.WriteLine($"Returned from long-running event with error: {Error}");
					this.Ended = true;
				}
			}

			public virtual void ParseGenericEvent(int correlationId, ushort eventId, byte eventType,
				ushort blockchainType, object message)
			{
				NLog.Default.Verbose($"Generic events not parsed for class {this.GetType()}");
			}

			public bool wait(int sleepStepMilliseconds = 100, bool resetFiledsOnCompletion = true)
			{
				bool result = false;
				while (true)
				{
					lock (this)
					{
						if (this.Ended)
						{
							result = !Error;
							break;
						}
					}
					Thread.Sleep(sleepStepMilliseconds);
				}
				
				if(resetFiledsOnCompletion)
					this.ResetFields();

				return result;
			}
		}
		private class AccountCreationStatus : LongRunningStatusBase
		{
			public bool Started = false;
		}


		public void AccountCreationStarted(int correlationId)
		{
			var status = GocLongRunningStatus<AccountCreationStatus>();
			lock (status)
			{
				status.Started = true;
			}
				
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}
		
		public void AccountCreationEnded(int correlationId)
		{
			var status = GocLongRunningStatus<AccountCreationStatus>();
			lock (status)
			{
				status.Ended = true;
			}
			
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}

		public void AccountCreationMessage(int correlationId, string message)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId, $"message: {message}\n");
		}

		public void AccountCreationStep(int correlationId, string stepName, int stepIndex, int stepTotal)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId, $"step: {stepName}, {stepIndex} out of {stepTotal}\n");
		}

		public void AccountCreationError(int correlationId, string error)
		{
			var status = GocLongRunningStatus<AccountCreationStatus>();
			lock (status)
			{
				status.Error = true;
			}
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId, $"Error was: {error}");
		}

		public void KeyGenerationStarted(int correlationId, ushort chainType, string keyName, int keyIndex, int keyTotal)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId, $"step: {keyName}, {keyIndex} out of {keyTotal}\n");
		}

		public void KeyGenerationEnded(int correlationId, ushort chainType, string keyName, int keyIndex, int keyTotal)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId, $"step: {keyName}, {keyIndex} out of {keyTotal}\n");
		}

		public void KeyGenerationError(int correlationId, ushort chainType, string keyName, string error)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId, $"Error for {keyName} was: {error}\n");
		}

		public void KeyGenerationPercentageUpdate(int correlationId, ushort chainType, string keyName, int percentage)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId, $"{keyName}: {percentage}%\n");
		}

		public void AccountPublicationStarted(int correlationId, ushort chainType)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}

		public void AccountPublicationEnded(int correlationId, ushort chainType)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}

		public void RequireNodeUpdate(ushort chainType, string chainName)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName());
		}

		public void AccountPublicationError(int correlationId, ushort chainType, string error)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId, $"Error was: {error}");
		}

		public void WalletSyncStarted(ushort chainType, long currentBlockId, long blockHeight, decimal percentage)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Block {currentBlockId} out of {blockHeight} ({percentage:P}%)");
		}

		public void WalletSyncEnded(ushort chainType, long currentBlockId, long blockHeight, decimal percentage)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Block {currentBlockId} out of {blockHeight} ({percentage:P}%)");
		}

		public void WalletSyncUpdate(ushort chainType, long currentBlockId, long blockHeight, decimal percentage,
			string estimatedTimeRemaining)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Block {currentBlockId} out of {blockHeight} ({percentage:P}%), {estimatedTimeRemaining} remaining");
		}

		public void WalletSyncError(ushort chainType, string error)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Error was: {error}");
		}

		public void BlockchainSyncStarted(ushort chainType, long currentBlockId, long publicBlockHeight, decimal percentage)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Block {currentBlockId} out of {publicBlockHeight} ({percentage:P}%)");
		}

		public void BlockchainSyncEnded(ushort chainType, long currentBlockId, long publicBlockHeight, decimal percentage)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Block {currentBlockId} out of {publicBlockHeight} ({percentage:P}%)");
		}

		public void BlockchainSyncUpdate(ushort chainType, long currentBlockId, long publicBlockHeight, decimal percentage,
			string estimatedTimeRemaining)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Block {currentBlockId} out of {publicBlockHeight} ({percentage:P}%)");
		}

		public void BlockchainSyncError(ushort chainType, string error)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Error was: {error}");
		}

		public void TransactionSent(int correlationId, ushort chainType, string transactionId)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}

		public void TransactionConfirmed(ushort chainType, string transactionId, object transaction)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"transaction id: {transactionId}, transaction: {transaction}");
		}

		public void TransactionReceived(ushort chainType, string transactionId)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"transaction id: {transactionId}");
		}

		public void TransactionMessage(ushort chainType, string transactionId, string message)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"transaction id: {transactionId}, message: {message}");
		}

		public void TransactionRefused(ushort chainType, string transactionId, string reason)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"transaction id {transactionId} refused, reason: {reason}");
		}

		public void MiningStarted(ushort chainType)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName());
		}

		public void MiningEnded(ushort chainType, int status)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"status: {status}");
		}

		public void MiningElected(ushort chainType, long electionBlockId, byte level)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"election block id: {electionBlockId}, level: {level}");
		}

		public void MiningPrimeElected(ushort chainType, long electionBlockId, byte level)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"election block id: {electionBlockId}, level: {level}");
		}

		public void MiningPrimeElectedMissed(ushort chainType, long publicationBlockId, long electionBlockId, byte level)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"election block id: {electionBlockId} (public {publicationBlockId}), level: {level}");
		}

		public void NeuraliumMiningBountyAllocated(ushort chainType, long blockId, decimal bounty, decimal transactionTip,
			string delegateAccountId)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"block id: {blockId}, bounty: {bounty}, tip: {transactionTip}");
		}

		public void NeuraliumMiningPrimeElected(ushort chainType, long electionBlockId, decimal bounty, decimal transactionTip,
			string delegateAccountId, byte level)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"election block id: {electionBlockId}, bounty: {bounty}, tip: {transactionTip}");
		}

		public void NeuraliumTimelineUpdated()
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName());
		}

		public void BlockInserted(ushort chainType, long blockId, DateTime timestamp, string hash, long publicBlockId, int lifespan)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"block id: {blockId} (public {publicBlockId}) at {timestamp}, hash: {hash}, lifespan: {lifespan}");
		}

		public void BlockInterpreted(ushort chainType, long blockId)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"block id: {blockId}");
		}

		public void DigestInserted(ushort chainType, int digestId, DateTime timestamp, string hash)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"digest id: {digestId} at {timestamp}, hash: {hash}");
		}

		public void ConsoleMessage(string message, DateTime timestamp, string level, object[] properties)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"message: {message} at {timestamp}, level: {level}, properties: [{string.Join(", ", properties)}]");
		}

		public void Error(int correlationId, ushort chainType, string error)
		{
			PrintEvent(LogLevel.Error, this.GetCallingMethodName(), error);
			ReturnClientLongRunningEvent(correlationId, -1, error);
		}

		public void Message(ushort chainType, ushort messageCode, string defaultMessage, string[] properties)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"code: {messageCode}, {defaultMessage}, properties: [{string.Join(", ", properties)}]");
		}

		public void Alert(ushort chainType, ushort messageCode, string defaultMessage, int priorityLevel, int reportLevel,
			string[] parameters)
		{
			PrintEvent(LogLevel.Warning, this.GetCallingMethodName(), $"code: {messageCode}, message: {defaultMessage}, priority: {priorityLevel}, level: {reportLevel}, parameters: [{string.Join(", ", parameters)}]");
		}

		public void ImportantWalletUpdate(ushort chainType)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName());
		}

		public void ConnectableStatusChanged(bool connectable)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Connectable: {connectable}");
		}

		public void ShutdownCompleted()
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName());
		}

		public void ShutdownStarted()
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName());
		}

		public void TransactionHistoryUpdated(ushort chainType)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName());
		}

		public void ElectionContextCached(ushort chainType, long blockId, long maturityId, long difficulty)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Block id: {blockId}, maturity: {maturityId}, difficulty: {difficulty}");
		}

		public void ElectionProcessingCompleted(ushort chainType, long blockId, int electionResultCount)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"Block id: {blockId}, {electionResultCount} results");
		}

		public void AppointmentVerificationCompleted(ushort chainType, bool verified, string appointmentConfirmationCode)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"Verified: {verified}, code {appointmentConfirmationCode}");
		}

		public void InvalidPuzzleEngineVersion(ushort chainType, int requiredVersion, int minimumSupportedVersion,
			int maximumSupportedVersion)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"Reuires {requiredVersion}, min {minimumSupportedVersion}, max {maximumSupportedVersion}");
		}

		public void THSTrigger(ushort chainType)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName());
		}

		public void THSBegin(ushort chainType, long difficulty, long targetNonce, long targetTotalDuration,
			long estimatedIterationTime, long estimatedRemainingTime, long startingNonce, long startingTotalNonce,
			long startingRound, long[] nonces, int[] solutions)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"difficulty: {difficulty}, target nonce: {targetNonce}, target duration: {targetTotalDuration}, remaining: {estimatedRemainingTime} ({estimatedIterationTime}/iteration), start nonce: {startingNonce}, start nonce (total): {startingTotalNonce}, start round: {startingRound}, nonces: [{string.Join(", ", nonces)}], solutions: [{string.Join(", ", solutions)}]");
		}

		public void THSRound(ushort chainType, int round, long totalNonce, long lastNonce, int lastSolution)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"round: {round}, last nonce: {lastNonce}, last solution: {lastSolution}");
		}

		public void THSIteration(ushort chainType, long[] nonces, long elapsed, long estimatedIterationTime,
			long estimatedRemainingTime, double benchmarkSpeedRatio)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"Nonces: [{string.Join(", ", nonces)}], elpased: {elapsed}, {estimatedRemainingTime} remaining ({estimatedIterationTime}/iteration), speed ratio: {benchmarkSpeedRatio}");
		}
		

		public void THSSolution(ushort chainType, List<long> nonces, List<int> solutions, long difficulty)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"nonces: [{string.Join(", ", nonces)}], solutions: [{string.Join(", ", solutions)}], difficulty: {difficulty}");
		}

		public void AppointmentPuzzleBegin(ushort chainType, int secretCode, List<string> puzzles, List<string> instructions)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"secret code: {secretCode}, {puzzles.Count} puzzles, {instructions.Count} instructions");
		}

		public void TransactionError(int correlationId, ushort chainType, string transactionId, List<ushort> errorCodes)
		{
			PrintEvent(LogLevel.Error, this.GetCallingMethodName(), correlationId, $"transaction id: '{transactionId}', error codes : [{string.Join(", ", errorCodes)}]");
		}

		public void CopyWalletKeyFile(int correlationId, int keyCorrelationCode, string accountCode, string keyname, int attempt) {
			NLog.Default.Information($"Event {this.GetCallingMethodName()} {correlationId} {keyCorrelationCode} {accountCode} {keyname} {attempt}");
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}
		
		public void RequestCopyWalletKeyFile(int correlationId, ushort chainType, int keyCorrelationCode, string accountCode,
			string keyname, int attempt)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}

		public void AccountTotalUpdated(string accountId, object total)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"account id: {accountId}, total: {total}");
		}

		public void RequestCopyWallet(int correlationId, ushort chainType)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}

		public void PeerTotalUpdated(int total)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"{total} peers");
		}

		public void BlockchainSyncStatusChanged(ushort chainType, Enums.ChainSyncState syncStatus)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"status: {syncStatus}");
		}

		public void WalletSyncStatusChanged(ushort chainType, Enums.ChainSyncState syncStatus)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"status: {syncStatus}");
		}

		public void MiningStatusChanged(ushort chainType, bool isMining)
		{
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"mining: {isMining}");
		}

		public void WalletTotalUpdated(int correlationId, string accountCode, double total) {
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"total: {total}");
		}

		/// <summary>
		///     a client triggered long running even has completed. take the return value and clear the cache
		/// </summary>
		/// <param name="correlationId"></param>
		/// <param name="result"></param>
		public void ReturnLongRunningEvent(int correlationId, bool result, string error) {
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"result: {result}, error: {error}");
		}

		public void LongRunningStatusUpdate(int correlationId, ushort eventId, byte eventType, string message) {
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), $"envent id {eventId}, evnet type: {eventType}, message: {message}");
		}

		public void AccountPublicationCompleted(int correlationId, string accountCode, bool result, long accountSequenceId, byte accountType) {
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), $"account code: {accountCode}, result: {result}, sequence id: {accountSequenceId}, type: {accountType}");
		}



		public void WalletCreationStarted(int correlationId)
		{
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}
		
		public void WalletCreationEnded(int correlationId)
		{
			var status = GocLongRunningStatus<LoadWalletStatus>();
			lock (status)
			{
				status.Error = false;
				status.Ended = true;
			}
			
			PrintEvent(LogLevel.Information, this.GetCallingMethodName(), correlationId);
		}



		public void ReturnClientLongRunningEvent(int correlationId, int result, string error) {
			
			PrintEvent(LogLevel.Verbose, this.GetCallingMethodName(), correlationId, $"result {result}, error: {error}");
			
			//TODO: is this still necessary?
			if(this.longRunningTasks.ContainsKey(correlationId)) {
				this.longRunningTasks.Remove(correlationId);
			}
			
			if(longRunningTasksCallbacks.ContainsKey(correlationId)) {
				longRunningTasksCallbacks[correlationId].Invoke(correlationId, result, error);
				longRunningTasksCallbacks.Remove(correlationId);
			}
		}

	#endregion

	#region Methods

		public async Task<bool> CompleteLongRunningEvent(int correlationId, object data) {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new[] {correlationId, data}).ConfigureAwait(false);
		}
		
		public async Task<bool> RenewLongRunningEvent(int correlationId) {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {correlationId}).ConfigureAwait(false);
		}
		

		public async Task<bool> IsBlockchainSynced()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> IsWalletSynced()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<int> GetCurrentOperatingMode()
		{
			return await this.signalrClient.InvokeMethod<int>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> SyncBlockchain(bool force)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType, force}).ConfigureAwait(false);
		}

		public async Task Test()
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[0]).ConfigureAwait(false);
		}

		public async Task<bool> Ping() {
			string result = await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new string[0]).ConfigureAwait(false);

			return result == "pong";
		}

		public async Task<object> GetPortMappingStatus()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {}).ConfigureAwait(false);
		}

		public async Task<bool> ConfigurePortMappingMode(bool useUPnP, bool usePmP, int natDeviceIndex)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {useUPnP, usePmP, natDeviceIndex}).ConfigureAwait(false);
		}

		public async Task<byte> GetPublicIPMode()
		{
			return await this.signalrClient.InvokeMethod<byte>(this.GetCallingMethodName(), new object[] {}).ConfigureAwait(false);
		}

		public async Task SetUILocale(string locale)
		{
			await this.signalrClient.InvokeMethod<byte>(this.GetCallingMethodName(), new object[] {locale}).ConfigureAwait(false);
		}

		public async Task<byte> GetMiningRegistrationIpMode()
		{
			return await this.signalrClient.InvokeMethod<byte>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> Shutdown() {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[0]).ConfigureAwait(false);
		}

		public async Task<object> BackupWallet()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> RestoreWalletFromBackup(string backupsPath, string passphrase, string salt, string nonce, int iterations)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {backupsPath, passphrase, salt, nonce, iterations}).ConfigureAwait(false);
		}

		public async Task<int> QueryTotalConnectedPeersCount()
		{
			return await this.signalrClient.InvokeMethod<int>(this.GetCallingMethodName(), new object[] {}).ConfigureAwait(false);
		}

		public async Task<List<object>> QueryPeerConnectionDetails()
		{
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {}).ConfigureAwait(false);
		}

		public async Task<bool> QueryMiningPortConnectable()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {}).ConfigureAwait(false);
		}

		public async Task<object> QueryWalletInfo()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<object> QueryBlockChainInfo() 
		{
			return await signalrClient.InvokeMethod<object>(GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> ClearAppointment(string accountCode)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		public async Task<object> CanPublishAccount(string accountCode) {
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		public async Task SetSMSConfirmationCode(string accountCode, long confirmationCode)
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType, accountCode, confirmationCode}).ConfigureAwait(false);
		}

		public async Task SetSMSConfirmationCodeString(string accountCode, string confirmationCode)
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType, accountCode, confirmationCode}).ConfigureAwait(false);
		}

		public async Task GenerateXmssKeyIndexNodeCache(string accountCode, byte ordinal, long index)
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType, accountCode, ordinal, index}).ConfigureAwait(false);
		}

		public async Task<bool> SetWalletPassphrase(int correlationId, string passphrase, bool setKeysToo = false)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {correlationId, passphrase, setKeysToo}).ConfigureAwait(false);
		}

		public async Task<bool> SetKeysPassphrase(int correlationId, string passphrase)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {correlationId, passphrase}).ConfigureAwait(false);
		}

		private class PublishAccountStatus : LongRunningStatusBase {}

		public async Task<bool> RequestAppointment(string accountCode, int preferredRegion)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {accountCode, preferredRegion}).ConfigureAwait(false);
		}

		public async Task<bool> PublishAccount(string accountCode) {
			var status = GocLongRunningStatus<SendNeuraliumsStatus>(true);
			
			await InvokeLongRunningMethod(GetCallingMethodName(), new object[] {chainType, accountCode}
				, status.ReturnClientLongRunningEventCallback ).ConfigureAwait(false);

			return status.wait(500);
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

		public async Task<bool> IsMiningAllowed()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> QueryBlockchainSynced()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> QueryWalletSynced()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<object> QueryElectionContext(long blockId) {
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<List<object>> QueryNeuraliumTransactionPool()
		{
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {}).ConfigureAwait(false);
		}

		public async Task<long> QueryLowestAccountBlockSyncHeight()
		{
			return await this.signalrClient.InvokeMethod<long>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<string> QueryBlock(long blockId) {
			return await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<object> QueryDecomposedBlock(long blockId)
		{
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<string> QueryDecomposedBlockJson(long blockId)
		{
			return await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<byte[]> QueryCompressedBlock(long blockId) {
			return await this.signalrClient.InvokeMethod<byte[]>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<byte[]> QueryBlockBytes(long blockId)
		{
			return await this.signalrClient.InvokeMethod<byte[]>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<string> GetBlockSizeAndHash(long blockId)
		{
			return await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		private class SendNeuraliumsStatus : LongRunningStatusBase {}

		public async Task<bool> CreateNextXmssKey(string accountCode, byte ordinal)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType, accountCode, ordinal}).ConfigureAwait(false);
		}

		public async Task<bool> SendNeuraliums(string targetAccountId, decimal amount, decimal tip, string note)
		{
			var status = GocLongRunningStatus<SendNeuraliumsStatus>(true);
			await InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {targetAccountId, amount, tip, note}
				, status.ReturnClientLongRunningEventCallback ).ConfigureAwait(false);

			return status.wait(500);
		}

		public async Task<object> QueryNeuraliumTimelineHeader(string accountCode)
		{
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {accountCode}).ConfigureAwait(false);
		}

		public async Task<object> QueryNeuraliumTimelineSection(string accountCode, DateTime day)
		{
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {accountCode, day}).ConfigureAwait(false);
		}

		public async Task<byte[]> SignXmssMessage(string accountCode, byte[] message)
		{
			return await this.signalrClient.InvokeMethod<byte[]>(this.GetCallingMethodName(), new object[] {chainType, accountCode, message}).ConfigureAwait(false);
		}

		public async Task SetPuzzleAnswers(List<int> answers)
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType, answers}).ConfigureAwait(false);
		}
#if COLORADO_EXCLUSION
		public async Task<bool> BypassAppointmentVerification(string accountCode) {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}
#endif
		public async Task<object> QueryElectionContext(ushort chainType, long blockId)
		{
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> RestoreWalletNarballBackup(string source, string dest)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType, source, dest}).ConfigureAwait(false);
		}

		public async Task EnterWalletPassphrase(int correlationId, int keyCorrelationCode, string passphrase)
		{
			var pwd = AskPassphrase(passphrase);
			
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {correlationId, chainType, keyCorrelationCode, pwd.ConvertToUnsecureString(), false}).ConfigureAwait(false);
		}

		public async Task<bool> ToggleServerMessages(bool enable)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {enable}).ConfigureAwait(false);
		}

		public async Task EnterKeyPassphrase(int correlationId, int keyCorrelationCode, string passphrase)
		{
			var pwd = AskPassphrase(passphrase);
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {correlationId, chainType, keyCorrelationCode, pwd.ConvertToUnsecureString()}).ConfigureAwait(false);
		}

		public async Task WalletKeyFileCopied(int correlationId, int keyCorrelationCode)
		{
			await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {correlationId, chainType, keyCorrelationCode}).ConfigureAwait(false);
		}

		public async Task<int> TestP2pPort(int testPort, bool callback)
		{
			return await this.signalrClient.InvokeMethod<int>(this.GetCallingMethodName(), new object[] {testPort, callback}).ConfigureAwait(false);
		}

		public async Task<List<object>> QuerySupportedChains() {
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[]{}).ConfigureAwait(false);

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

		private class LoadWalletStatus : LongRunningStatusBase
		{
			public override void ParseGenericEvent(int correlationId, ushort eventId, byte eventType,
				ushort blockchainType, object message)  
			{
				if (eventId == BlockchainSystemEventTypes.Instance.WalletLoadingError.Value)
				{
					lock (this)
					{
						this.Error = true;
						this.Ended = true;
						this.ErrorMessage = "WalletLoadingError";
					}
				}
			}
		}
		
		public async Task<bool> LoadWallet(string passphrase)
		{
			var status = GocLongRunningStatus<LoadWalletStatus>(true);

			int correlationId = await this.InvokeLongRunningMethod(this.GetCallingMethodName()
				, new object[] {chainType, passphrase}, status.ReturnClientLongRunningEventCallback).ConfigureAwait(false);
			return status.wait(100);
		}

		private static SecureString AskPassphrase(string passphrase, bool verify = false, bool hide = true, string message = "Please enter your passphrase, the press 'Enter':")
		{
			var pwd = new SecureString();
			if (passphrase == null)
			{
				
				Console.WriteLine(message);
				while (true)
				{
					ConsoleKeyInfo i = Console.ReadKey(true);
					if (i.Key == ConsoleKey.Enter)
						break;
					
					if (i.Key == ConsoleKey.Backspace)
					{
						if (pwd.Length > 0)
						{
							pwd.RemoveAt(pwd.Length - 1);
							Console.Write("\b \b");
						}
					}
					else if (i.KeyChar != '\u0000' ) // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
					{
						pwd.AppendChar(i.KeyChar);
						
						Console.Write(hide ? "*" : i.KeyChar);
					}
				}

				if (verify)
				{
					Console.WriteLine("");
					var pwdVerif = AskPassphrase(null, false, hide, "Now re-enter the same passphrase, the press 'Enter':");
					if (!pwd.SecureStringEqual(pwdVerif))
					{
						Console.WriteLine("Passphrases don't match, starting over without hiding");
						pwd = AskPassphrase(null, true, false);
					}
				}
				
				Console.WriteLine("");
			}
			else
			{
				foreach (char c in passphrase)
					pwd.AppendChar(c);

			}
			pwd.MakeReadOnly();
			return pwd;
		}

		public async Task<bool> WalletExists() {
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}
		

		public async Task<bool> CreateNewWallet(string accountName, int accountType, bool encryptWallet, bool encryptKey, bool encryptKeysIndividually)
		{

			
			var mutablePassphrases = AskPassphrases(encryptWallet, encryptKey, encryptKeysIndividually);

			var status = GocLongRunningStatus<AccountCreationStatus>(true);
			int correlationId = await this.InvokeLongRunningMethod(this.GetCallingMethodName()
				, new object[] {chainType, accountName, accountType, encryptWallet, encryptKey, encryptKeysIndividually, mutablePassphrases.ToImmutableDictionary(), false}
				, status.ReturnClientLongRunningEventCallback).ConfigureAwait(false);
			return status.wait(500);
			
		}

		private static Dictionary<int, string> AskPassphrases(bool encryptWallet, bool encryptKey, bool encryptKeysIndividually)
		{
			var mutablePassphrases = new Dictionary<int, string>();

			if (encryptWallet)
			{
				Console.WriteLine("You enabled wallet encryption, choose a passphrase for key index 0.");

				var pwd = AskPassphrase(null, true);
				mutablePassphrases.Add(0, pwd.ConvertToUnsecureString());
			}

			if (encryptKey && !encryptKeysIndividually)
			{
				Console.WriteLine("You enabled keys encryption, choose a passphrase for key index 1 (all keys).");
				var pwd = AskPassphrase(null, true);
				mutablePassphrases.Add(1, pwd.ConvertToUnsecureString());
			}

			if (encryptKey && encryptKeysIndividually)
			{
				Console.WriteLine(
					"You enabled individual keys encryption, choose a passphrase for key index 1 (transaction key).");
				var pwd = AskPassphrase(null, true);
				mutablePassphrases.Add(1, pwd.ConvertToUnsecureString());
			}

			if (encryptKey && encryptKeysIndividually)
			{
				Console.WriteLine(
					"You enabled individual keys encryption, choose a passphrase for key index 2 (messages key).");
				var pwd = AskPassphrase(null, true);
				mutablePassphrases.Add(2, pwd.ConvertToUnsecureString());
			}

			if (encryptKey && encryptKeysIndividually)
			{
				Console.WriteLine(
					"You enabled individual keys encryption, choose a passphrase for key index 3 (key change key).");
				var pwd = AskPassphrase(null, true);
				mutablePassphrases.Add(3, pwd.ConvertToUnsecureString());
			}

			if (encryptKey && encryptKeysIndividually)
			{
				Console.WriteLine(
					"You enabled individual keys encryption, choose a passphrase for key index 4 (super key).");
				var pwd = AskPassphrase(null, true);
				mutablePassphrases.Add(4, pwd.ConvertToUnsecureString());
			}

			if (encryptKey && encryptKeysIndividually)
			{
				Console.WriteLine(
					"You enabled individual keys encryption, choose a passphrase for key index 5 (validator key).");
				var pwd = AskPassphrase(null, true);
				mutablePassphrases.Add(5, pwd.ConvertToUnsecureString());
			}

			if (encryptKey && encryptKeysIndividually)
			{
				Console.WriteLine(
					"You enabled individual keys encryption, choose a passphrase for key index 6 (secret key).");
				var pwd = AskPassphrase(null, true);
				mutablePassphrases.Add(6, pwd.ConvertToUnsecureString());
			}

			return mutablePassphrases;
		}

		public async Task<List<object>> QueryWalletTransactionHistory(string accountCode) {
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		public async Task<object> QueryWalletTransactionHistoryDetails(string accountCode, string transactionId)
		{
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType, accountCode, transactionId}).ConfigureAwait(false);
		}

		public async Task<List<object>> QueryWalletAccounts() {
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<string> QueryDefaultWalletAccountId()
		{
			return await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<string> QueryDefaultWalletAccountCode()
		{
			return await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<object> QueryWalletAccountDetails(string accountCode)
		{
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		public async Task<object> QuerySystemInfo()
		{
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {}).ConfigureAwait(false);
		}
		
		public async Task<object> QueryWalletAccountAppointmentDetails(string accountCode)
		{
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		public async Task<string> QueryWalletAccountPresentationTransactionId(string accountCode)
		{
			return await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		public async Task<string> Test(string data)
		{
			return await this.signalrClient.InvokeMethod<string>(this.GetCallingMethodName(), new object[] {data}).ConfigureAwait(false);
		}

		private class PresentAccountPubliclyStatus : LongRunningStatusBase { }

		public async Task<bool> PresentAccountPublicly()
		{
			var status = GocLongRunningStatus<PresentAccountPubliclyStatus>(true);
			
			await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {chainType}
				, status.ReturnClientLongRunningEventCallback ).ConfigureAwait(false);

			return status.wait(500);
		}

		public async Task<List<object>> QueryBlockBinaryTransactions(long blockId) {
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType, blockId}).ConfigureAwait(false);
		}

		public async Task<bool> CreateStandardAccount(string accountName, int accountType, bool publishAccount, bool encryptKeys,
			bool encryptKeysIndividually)
		{
			var mutablePassphrases = AskPassphrases(false, encryptKeys, encryptKeysIndividually);
			
			var status = GocLongRunningStatus<AccountCreationStatus>(true);
			int correlationId = await this.InvokeLongRunningMethod(this.GetCallingMethodName()
				, new object[] {chainType, accountName, accountType, publishAccount, encryptKeys, encryptKeysIndividually, mutablePassphrases.ToImmutableDictionary()}
				, status.ReturnClientLongRunningEventCallback).ConfigureAwait(false);
			return status.wait(500);
		}

		public async Task<bool> SetActiveAccount(string accountCode)
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		public async Task<object> QueryAppointmentConfirmationResult(string accountCode)
		{
			return await this.signalrClient.InvokeMethod(this.GetCallingMethodName(), new object[] {chainType, accountCode}).ConfigureAwait(false);
		}

		#endregion

	#region Neuralium chain methods

		public async Task<object> QueryAccountTotalNeuraliums(string accountCode) {
			return await this.signalrClient.InvokeMethod<object>(this.GetCallingMethodName(), new object[] {accountCode}).ConfigureAwait(false);
		}

		public async Task<List<object>> QueryMiningHistory(int page, int pageSize, byte maxLevel)
		{
			return await this.signalrClient.InvokeMethod<List<object>>(this.GetCallingMethodName(), new object[] {chainType, page, pageSize, maxLevel}).ConfigureAwait(false);
		}

		public async Task<object> QueryMiningStatistics()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<bool> ClearCachedCredentials()
		{
			return await this.signalrClient.InvokeMethod<bool>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		public async Task<long> QueryCurrentDifficulty()
		{
			return await this.signalrClient.InvokeMethod<long>(this.GetCallingMethodName(), new object[] {chainType}).ConfigureAwait(false);
		}

		private class SendNeuraliumStatus : LongRunningStatusBase { }
		public async Task<bool> SendNeuraliums(long recipientAccountId, double amount, double fees)
		{

			var status = GocLongRunningStatus<SendNeuraliumStatus>();
			await this.InvokeLongRunningMethod(this.GetCallingMethodName(), new object[] {recipientAccountId, amount, fees}				
				, status.ReturnClientLongRunningEventCallback ).ConfigureAwait(false);

			return status.wait(500);
		}

	#endregion

	}

}
