using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neuralium.Cli.Classes.API {
	public interface IApiEvents {
		void ReturnClientLongRunningEvent(int correlationId, int result, string error);
		void LongRunningStatusUpdate(int correlationId, ushort eventId, byte eventType, ushort blockchainType, object message);

		void EnterWalletPassphrase(int correlationId, ushort chainType, int keyCorrelationCode, int attempt);
		void EnterKeysPassphrase(int correlationId, ushort chainType, int keyCorrelationCode, string accountCode, string keyname, int attempt);
		void RequestCopyWalletKeyFile(int correlationId, ushort chainType, int keyCorrelationCode, string accountCode, string keyname, int attempt);

		void AccountTotalUpdated(string accountId, object total);
		void RequestCopyWallet(int correlationId, ushort chainType);
		void PeerTotalUpdated(int total);

		// void BlockchainSyncStatusChanged(ushort chainType, Enums.ChainSyncState syncStatus);
		// void WalletSyncStatusChanged(ushort chainType, Enums.ChainSyncState syncStatus);

		void MiningStatusChanged(ushort chainType, bool isMining);

		void walletCreationStarted(int correlationId);
		void WalletCreationEnded(int correlationId);

		void AccountCreationStarted(int correlationId);
		void AccountCreationEnded(int correlationId, string accountCode);
		
		void AccountCreationMessage(int correlationId, string message);
		void AccountCreationStep(int correlationId, string stepName, int stepIndex, int stepTotal);
		void AccountCreationError(int correlationId, string error);

		void KeyGenerationStarted(int correlationId, ushort chainType, string keyName, int keyIndex, int keyTotal);
		void KeyGenerationEnded(int correlationId, ushort chainType, string keyName, int keyIndex, int keyTotal);
		void KeyGenerationError(int correlationId, ushort chainType, string keyName, string error);
		void KeyGenerationPercentageUpdate(int correlationId, ushort chainType, string keyName, int percentage);

		void AccountPublicationStarted(int correlationId, ushort chainType);
		void AccountPublicationEnded(int correlationId, ushort chainType);
		void RequireNodeUpdate(ushort chainType, string chainName);
		
		void AccountPublicationError(int correlationId, ushort chainType, string error);

		void WalletSyncStarted(ushort chainType, long currentBlockId, long blockHeight, decimal percentage);
		void WalletSyncEnded(ushort chainType, long currentBlockId, long blockHeight, decimal percentage);
		void WalletSyncUpdate(ushort chainType, long currentBlockId, long blockHeight, decimal percentage, string estimatedTimeRemaining);
		void WalletSyncError(ushort chainType, string error);

		void BlockchainSyncStarted(ushort chainType, long currentBlockId, long publicBlockHeight, decimal percentage);
		void BlockchainSyncEnded(ushort chainType, long currentBlockId, long publicBlockHeight, decimal percentage);
		void BlockchainSyncUpdate(ushort chainType, long currentBlockId, long publicBlockHeight, decimal percentage, string estimatedTimeRemaining);
		void BlockchainSyncError(ushort chainType, string error);

		void TransactionSent(int correlationId, ushort chainType, string transactionId);
		void TransactionConfirmed(ushort chainType, string transactionId, object transaction);
		void TransactionReceived(ushort chainType, string transactionId);
		void TransactionMessage(ushort chainType, string transactionId, string message);
		void TransactionRefused(ushort chainType, string transactionId, string reason);
		void TransactionError(int correlationId, ushort chainType, string transactionId, List<ushort> errorCodes);

		void MiningStarted(ushort chainType);
		void MiningEnded(ushort chainType, int status);
		void MiningElected(ushort chainType, long electionBlockId, byte level);
		void MiningPrimeElected(ushort chainType, long electionBlockId, byte level);
		void MiningPrimeElectedMissed(ushort chainType, long publicationBlockId, long electionBlockId, byte level);

		void NeuraliumMiningBountyAllocated(ushort chainType, long blockId, decimal bounty, decimal transactionTip, string delegateAccountId);
		void NeuraliumMiningPrimeElected(ushort chainType, long electionBlockId, decimal bounty, decimal transactionTip, string delegateAccountId, byte level);
		void NeuraliumTimelineUpdated();

		void BlockInserted(ushort chainType, long blockId, DateTime timestamp, string hash, long publicBlockId, int lifespan);
		void BlockInterpreted(ushort chainType, long blockId);

		void DigestInserted(ushort chainType, int digestId, DateTime timestamp, string hash);

		void ConsoleMessage(string message, DateTime timestamp, string level, object[] properties);
		void Error(int correlationId, ushort chainType, string error);

		void Message(ushort chainType, ushort messageCode, string defaultMessage, string[] properties);
		void Alert(ushort chainType, ushort messageCode, string defaultMessage, int priorityLevel, int reportLevel, string[] parameters);
		void ImportantWalletUpdate(ushort chainType);
		
		void ConnectableStatusChanged(bool connectable);

		void ShutdownCompleted();
		void ShutdownStarted();

		void TransactionHistoryUpdated(ushort chainType);

		void ElectionContextCached(ushort chainType, long blockId, long maturityId, long difficulty);
		void ElectionProcessingCompleted(ushort chainType, long blockId, int electionResultCount);

		void AppointmentPuzzleBegin(ushort chainType, int secretCode, List<string> puzzles, List<string> instructions);
		void AppointmentVerificationCompleted(ushort chainType, bool verified, string appointmentConfirmationCode);
		void InvalidPuzzleEngineVersion(ushort chainType, int requiredVersion, int minimumSupportedVersion, int maximumSupportedVersion);

		void THSTrigger(ushort chainType);
		void THSBegin(ushort chainType, long difficulty, long targetNonce, long targetTotalDuration, long estimatedIterationTime, long estimatedRemainingTime, long startingNonce, long startingTotalNonce, long startingRound, long[] nonces, int[]solutions);
		void THSRound(ushort chainType, int round, long totalNonce, long lastNonce, int lastSolution);
		void THSIteration(ushort chainType, long[] nonces, long elapsed, long estimatedIterationTime, long estimatedRemainingTime, double benchmarkSpeedRatio);
		void THSSolution(ushort chainType, List<long> nonces, List<int> solutions, long difficulty);

	}
}