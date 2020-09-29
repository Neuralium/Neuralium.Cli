using System;

namespace Neuralium.Cli.Classes.API {
	public interface IApiEvents {

		void EnterWalletPassphrase(int correlationId, int keyCorrelationCode, int attempt);
		void EnterKeysPassphrase(int correlationId, int keyCorrelationCode, string accountCode, string keyname, int attempt);
		void CopyWalletKeyFile(int correlationId, int keyCorrelationCode, string accountCode, string keyname, int attempt);

		void ReturnClientLongRunningEvent(int correlationId, int result, string error);
		void RequestCopyWallet(string path);
		void EnterWalletPassphrase(int correlationId, string accountCode, string path);
		void EnterWalletKeyPassphrase(string accountCode, string path);

		void walletCreationStarted(int correlationId);
		void WalletCreationEnded(int correlationId);
		
		void LongRunningStatusUpdate(int correlationId, ushort eventId, byte eventType, string message);

		void AccountPublicationCompleted(int correlationId, string accountCode, bool result, long accountSequenceId, byte accountType);

		// neuraliums
		void WalletTotalUpdated(int correlationId, string accountCode, double total);
	}
}