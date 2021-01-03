using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Neuralium.Cli.Classes.API {
	public interface IApiMethods {

		Task EnterWalletPassphrase(uint correlationId, uint keyCorrelationCode, string passphrase);
		Task EnterKeyPassphrase(uint correlationId, uint keyCorrelationCode, string passphrase);
		Task WalletKeyFileCopied(uint correlationId, uint keyCorrelationCode);

		Task<object> QuerySupportedChains();
		Task<long> QueryBlockHeight();

		Task<object> QueryChainStatus();
		Task<bool> IsWalletLoaded();
		Task<uint> LoadWallet(string passphrase);
		Task<bool> WalletExists();
		Task<bool> CompleteLongRunningEvent(uint correlationId, object data);
		Task<bool> RenewLongRunningEvent(uint correlationId);
		Task Test();
		Task<bool> Ping();
		Task<bool> Shutdown();

		Task<object> QueryBlockChainInfo();
		Task<uint> CreateNewWallet(string accountName, int accountType, bool encryptWallet, bool encryptKey, bool encryptKeysIndividually, ImmutableDictionary<string, string> passphrases, bool publishAccount);
		Task<List<object>> QueryBlockBinaryTransactions(long blockId);

		Task<object> CanPublishAccount(string accountCode);
		
		Task<uint> PublishAccount(string accountCode);

		Task<object> QueryElectionContext(long blockId);

		Task StartMining(string delegateAccountId, int tier = 0);
		Task StopMining();
		Task<bool> IsMiningEnabled();

		Task<string> QueryBlock(long blockId);

		Task<byte[]> QueryCompressedBlock(long blockId);

		Task<List<object>> QueryWalletTransactionHistory(string accountCode);
		Task<List<object>> QueryWalletAccounts();

		Task<uint> PresentAccountPublicly();

		// neuralium chain
		Task<object> QueryAccountTotalNeuraliums(string accountCode);
		Task<uint> SendNeuraliums(string targetAccountId, decimal amount, decimal tip, string note);
	}
}