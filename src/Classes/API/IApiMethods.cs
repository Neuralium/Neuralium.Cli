using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		Task<int> LoadWallet();
		Task<bool> WalletExists();
		Task<bool> CompleteLongRunningEvent(int correlationId, object data);
		Task<bool> RenewLongRunningEvent(int correlationId);
		Task Test();
		Task<bool> Ping();
		Task<bool> Shutdown();

		Task<object> QueryBlockChainInfo();
		Task<int> CreateNewWallet(string accountName, int accountType, bool encryptWallet, bool encryptKey, bool encryptKeysIndividually, ImmutableDictionary<string, string> passphrases, bool publishAccount);
		Task<List<object>> QueryBlockBinaryTransactions(long blockId);

		Task<object> CanPublishAccount(string accountCode);
		
		Task<int> PublishAccount(string accountCode);

		Task<object> QueryElectionContext(long blockId);

		Task StartMining(string delegateAccountId, int tier = 0);
		Task StopMining();
		Task<bool> IsMiningEnabled();

		Task<string> QueryBlock(long blockId);

		Task<byte[]> QueryCompressedBlock(long blockId);

		Task<List<object>> QueryWalletTransactionHistory(string accountCode);
		Task<List<object>> QueryWalletAccounts();

		Task<int> PresentAccountPublicly();

		// neuralium chain
		Task<object> QueryAccountTotalNeuraliums(string accountCode);
		Task<int> SendNeuraliums(string targetAccountId, decimal amount, decimal tip, string note);
	}
}