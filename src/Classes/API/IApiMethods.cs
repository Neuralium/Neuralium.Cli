using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neuralium.Cli.Classes.API {
	public interface IApiMethods {

		Task EnterWalletPassphrase(int correlationId, int keyCorrelationCode, string passphrase);
		Task EnterKeyPassphrase(int correlationId, int keyCorrelationCode, string passphrase);
		Task WalletKeyFileCopied(int correlationId, int keyCorrelationCode);

		Task<object> QuerySupportedChains();
		Task<long> QueryBlockHeight();

		Task<object> QueryChainStatus();
		Task<bool> IsWalletLoaded();
		Task LoadWallet();
		Task<bool> WalletExists();
		Task<bool> CompleteLongRunningEvent(int correlationId, object data);
		Task<bool> RenewLongRunningEvent(int correlationId);
		Task Test();
		Task<bool> Ping();
		Task<bool> Shutdown();

		Task<object> QueryBlockChainInfo();
		Task<int> CreateNewWallet(string accountName, bool encryptWallet, bool encryptKey, bool encryptKeysIndividually, Dictionary<int, string> passphrases, bool publishAccount);
		
		Task<int> PublishAccount(string accountUuId);

		Task StartMining(string delegateAccountId);
		Task StopMining();
		
		Task<string> QueryBlock(long blockId);

		Task<byte[]> QueryCompressedBlock(long blockId);


			Task<List<object>> QueryWalletTransactionHistory(Guid accountUuid);
		Task<List<object>> QueryWalletAccounts();

		Task PresentAccountPublicly();

		// neuralium chain
		Task<object> QueryAccountTotalNeuraliums(Guid accountUuid);
		Task SendNeuraliums(long recipientAccountId, double amount, double fees);
	}
}