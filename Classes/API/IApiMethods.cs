using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Neuralia.Blockchains.Core;

namespace Neuralium.Cli.Classes.API {
	
	public interface IApiMethods {

		Task<bool> Login(string user, string password);
		Task<bool> ToggleServerMessages(bool enable);
		Task EnterWalletPassphrase(int correlationId, int keyCorrelationCode, string passphrase, bool setKeysToo = false);
		Task EnterKeyPassphrase(int correlationId, int keyCorrelationCode, string passphrase);
		Task WalletKeyFileCopied(int correlationId, int keyCorrelationCode);

		Task<int> TestP2pPort(int testPort, bool callback);
		
		Task<object> QuerySystemInfo();
		Task<List<object>> QuerySupportedChains();
		Task<bool> Ping();
		Task<object> GetPortMappingStatus();
		Task<bool> ConfigurePortMappingMode(bool useUPnP, bool usePmP, int natDeviceIndex);

		Task<byte> GetPublicIPMode();

		Task SetUILocale(string locale);
		Task<byte> GetMiningRegistrationIpMode();

		Task<bool> CompleteLongRunningEvent(int correlationId, object data);
		Task<bool> RenewLongRunningEvent(int correlationId);

		Task<bool> IsBlockchainSynced();
		Task<bool> IsWalletSynced();

		Task<int> GetCurrentOperatingMode();
		Task<bool> SyncBlockchain(bool force);
		Task<bool> Shutdown();
		Task<object> BackupWallet();
		Task<bool> RestoreWalletFromBackup(string backupsPath, string passphrase, string salt, string nonce, int iterations, bool legacyBase32);
		Task<bool> AttemptWalletRescue();
		
		Task<int> QueryTotalConnectedPeersCount();
		Task<List<object>> QueryPeerConnectionDetails();
		Task<bool> DynamicPeerOperation(string ip, int port, int operation);
		Task<bool> QueryMiningPortConnectable();
		Task<object> QueryChainStatus();
		Task<object> QueryWalletInfo();

		Task<object> QueryBlockChainInfo();

		Task<bool> IsWalletLoaded();
		Task<bool> WalletExists();
		Task<bool> LoadWallet(string passphrase = null);
		Task<long> QueryBlockHeight();
		Task<int> QueryDigestHeight();
		Task<bool> ResetWalletIndex();
		Task<long> QueryLowestAccountBlockSyncHeight();
		Task<string> QueryBlock(long blockId);
		Task<object> QueryDecomposedBlock(long blockId);
		Task<string> QueryDecomposedBlockJson(long blockId);
		Task<byte[]> QueryCompressedBlock(long blockId);
		Task<byte[]> QueryBlockBytes(long blockId);
		Task<string> GetBlockSizeAndHash(long blockId);
		Task<List<object>> QueryBlockBinaryTransactions(long blockId);
		Task<bool> CreateStandardAccount(string accountName, int accountType, bool publishAccount, bool encryptKeys, bool encryptKeysIndividually);
		Task<bool> SetActiveAccount(string accountCode);
		Task<object> QueryAppointmentConfirmationResult(string accountCode);
		Task<bool> ClearAppointment(string accountCode);
		Task<object> CanPublishAccount(string accountCode);
		Task SetSMSConfirmationCode(string accountCode, long confirmationCode);
		Task SetSMSConfirmationCodeString(string accountCode, string confirmationCode);

		Task GenerateXmssKeyIndexNodeCache(string accountCode, byte ordinal, long index);
		Task<bool> CreateNewWallet(string accountName, int accountType, bool encryptWallet, bool encryptKey, bool encryptKeysIndividually);

		Task<bool> SetWalletPassphrase(int correlationId, string passphrase, bool setKeysToo = false);
		Task<bool> SetKeysPassphrase(int correlationId, string passphrase);

		Task<List<object>> QueryWalletTransactionHistory(string accountCode);
		Task<object> QueryWalletTransactionHistoryDetails(string accountCode, string transactionId);
		Task<List<object>> QueryWalletAccounts();
		Task<string> QueryDefaultWalletAccountId();
		Task<string> QueryDefaultWalletAccountCode();
		Task<object> QueryWalletAccountDetails(string accountCode);
		Task<object> QueryWalletAccountAppointmentDetails(string accountCode);
		Task<string> QueryWalletAccountPresentationTransactionId(string accountCode);

		Task<string> Test(string data);
		Task<bool> RequestAppointment(string accountCode, int preferredRegion);
		Task<bool> PublishAccount(string accountCode);
		Task StartMining(string delegateAccountId, int tier = 0);
		Task StopMining();
		Task<bool> IsMiningEnabled();
		Task<bool> IsMiningAllowed();
		Task<bool> QueryBlockchainSynced();
		Task<bool> QueryWalletSynced();
		Task<string> GenerateTestPuzzle();
		Task<object> QueryAccountTotalNeuraliums(string accountCode);
		Task<List<object>> QueryMiningHistory(int page, int pageSize, byte maxLevel);
		Task<object> QueryMiningStatistics();
		Task<bool> ClearCachedCredentials();

		Task<long> QueryCurrentDifficulty();

		Task<bool> CreateNextXmssKey(string accountCode, byte ordinal);

		Task<bool> SendNeuraliums(string targetAccountId, decimal amount, decimal fees, string note);
		Task<object> QueryNeuraliumTimelineHeader(string accountCode);
		Task<object> QueryNeuraliumTimelineSection(string accountCode, DateTime day);

		Task<byte[]> SignXmssMessage(string accountCode, byte[] message);

		Task SetPuzzleAnswers(List<int> answers);

#if TESTNET || DEVNET
		Task<int> RefillNeuraliums(string accountCode);
#endif
#if COLORADO_EXCLUSION
		Task<bool> BypassAppointmentVerification(string accountCode);
#endif

		Task<object> QueryElectionContext(long blockId);
		Task<List<object>> QueryNeuraliumTransactionPool();
		Task<bool> RestoreWalletNarballBackup(string source, string dest);
		Task<object> ReadAppSetting(string name);
		Task<bool> WriteAppSetting(string name, string value);
		Task<object> ReadAppSettingDomain(string name);
	}
}