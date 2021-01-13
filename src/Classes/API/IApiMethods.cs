using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Neuralium.Cli.Classes.API {
	public interface IApiMethods {

		public Task<bool> ToggleServerMessages(bool enable);
		public Task EnterWalletPassphrase(int correlationId, int keyCorrelationCode, string passphrase);
		public Task EnterKeyPassphrase(int correlationId, int keyCorrelationCode, string passphrase);
		public Task WalletKeyFileCopied(int correlationId, int keyCorrelationCode);

		public Task<bool> TestP2pPort(int testPort, bool callback);
		
		public Task<object> QuerySystemInfo();
		public Task<List<object>> QuerySupportedChains();
		public Task<bool> Ping();
		public Task<object> GetPortMappingStatus();
		public Task<bool> ConfigurePortMappingMode(bool useUPnP, bool usePmP, int natDeviceIndex);

		public Task<byte> GetPublicIPMode();

		public Task SetUILocale(string locale);
		public Task<byte> GetMiningRegistrationIpMode();

		public Task<bool> CompleteLongRunningEvent(int correlationId, object data);
		public Task<bool> RenewLongRunningEvent(int correlationId);

		public Task<bool> IsBlockchainSynced();
		public Task<bool> IsWalletSynced();

		public Task<int> GetCurrentOperatingMode();
		public Task<bool> SyncBlockchain(bool force);
		public Task<bool> Shutdown();
		public Task<object> BackupWallet();
		public Task<bool> RestoreWalletFromBackup(string backupsPath, string passphrase, string salt, string nonce, int iterations);

		public Task<int> QueryTotalConnectedPeersCount();
		
		public Task<List<object>> QueryPeerConnectionDetails();
		
		public Task<bool> QueryMiningPortConnectable();
		public Task<object> QueryChainStatus();
		public Task<object> QueryWalletInfo();

		public Task<object> QueryBlockChainInfo();

		public Task<bool> IsWalletLoaded();
		public Task<bool> WalletExists();
		public Task<bool> LoadWallet(string passphrase = null);
		public Task<long> QueryBlockHeight();
		public Task<long> QueryLowestAccountBlockSyncHeight();
		public Task<string> QueryBlock(long blockId);
		Task<object> QueryDecomposedBlock(long blockId);
		Task<string> QueryDecomposedBlockJson(long blockId);
		public Task<byte[]> QueryCompressedBlock(long blockId);
		public Task<byte[]> QueryBlockBytes(long blockId);
		public Task<string> GetBlockSizeAndHash(long blockId);
		public Task<List<object>> QueryBlockBinaryTransactions(long blockId);
		public Task<bool> CreateStandardAccount(string accountName, int accountType, bool publishAccount, bool encryptKeys, bool encryptKeysIndividually);
		public Task<bool> SetActiveAccount(string accountCode);
		public Task<object> QueryAppointmentConfirmationResult(string accountCode);
		public Task<bool> ClearAppointment(string accountCode);
		public Task<object> CanPublishAccount(string accountCode);
		public Task SetSMSConfirmationCode(string accountCode, long confirmationCode);
		public Task SetSMSConfirmationCodeString(string accountCode, string confirmationCode);


		public Task GenerateXmssKeyIndexNodeCache(string accountCode, byte ordinal, long index);
		public Task<bool> CreateNewWallet(string accountName, int accountType, bool encryptWallet, bool encryptKey, bool encryptKeysIndividually);

		public Task<bool> SetWalletPassphrase(int correlationId, string passphrase, bool setKeysToo = false);
		public Task<bool> SetKeysPassphrase(int correlationId, string passphrase);

		public Task<List<object>> QueryWalletTransactionHistory(string accountCode);
		public Task<object> QueryWalletTransactionHistoryDetails(string accountCode, string transactionId);
		public Task<List<object>> QueryWalletAccounts();
		public Task<string> QueryDefaultWalletAccountId();
		public Task<string> QueryDefaultWalletAccountCode();
		public Task<object> QueryWalletAccountDetails(string accountCode);
		public Task<object> QueryWalletAccountAppointmentDetails(string accountCode);
		public Task<string> QueryWalletAccountPresentationTransactionId(string accountCode);

		public Task<string> Test(string data);
		public Task<bool> RequestAppointment(string accountCode, int preferredRegion);
		public Task<bool> PublishAccount(string accountCode);
		public Task StartMining(string delegateAccountId, int tier = 0);
		public Task StopMining();
		public Task<bool> IsMiningEnabled();
		public Task<bool> IsMiningAllowed();
		public Task<bool> QueryBlockchainSynced();
		public Task<bool> QueryWalletSynced();
		public Task<object> QueryAccountTotalNeuraliums(string accountCode);
		public Task<List<object>> QueryMiningHistory(int page, int pageSize, byte maxLevel);
		public Task<object> QueryMiningStatistics();
		public Task<bool> ClearCachedCredentials();

		public Task<long> QueryCurrentDifficulty();

		public Task<bool> CreateNextXmssKey(string accountCode, byte ordinal);

		public Task<bool> SendNeuraliums(string targetAccountId, decimal amount, decimal fees, string note);
		public Task<object> QueryNeuraliumTimelineHeader(string accountCode);
		public Task<object> QueryNeuraliumTimelineSection(string accountCode, DateTime day);

		public Task<byte[]> SignXmssMessage(string accountCode, byte[] message);

		public Task SetPuzzleAnswers(List<int> answers);

		
	}
}