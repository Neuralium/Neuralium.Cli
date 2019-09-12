using System;

namespace Neuralium.Cli.Classes.API {
	public interface IApiEvents {

		void EnterWalletPassphrase(int correlationId, int keyCorrelationCode, int attempt);
		void EnterKeysPassphrase(int correlationId, int keyCorrelationCode, Guid accountID, string keyname, int attempt);
		void CopyWalletKeyFile(int correlationId, int keyCorrelationCode, Guid accountID, string keyname, int attempt);

		void ReturnClientLongRunningEvent(int correlationId, int result, string error);
		void RequestCopyWallet(string path);
		void EnterWalletPassphrase(int correlationId, Guid accountID, string path);
		void EnterWalletKeyPassphrase(Guid accountID, string path);

		void LongRunningStatusUpdate(int correlationId, ushort eventId, byte eventType, string message);

		void AccountPublicationCompleted(int correlationId, Guid accountUuid, bool result, long accountSequenceId, byte accountType);

		// neuraliums
		void WalletTotalUpdated(int correlationId, Guid accountId, double total);
	}
}