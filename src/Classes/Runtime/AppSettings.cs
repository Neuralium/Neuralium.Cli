

using Neuralia.Blockchains.Core.Services;

namespace Neuralium.Cli.Classes.Runtime {
	public class AppSettings {
		public bool UseTls { get; set; } = false;

		public int RpcPort { get; } = GlobalsService.DEFAULT_RPC_PORT;

		public string Host { get; set; } = "localhost";
	}

}