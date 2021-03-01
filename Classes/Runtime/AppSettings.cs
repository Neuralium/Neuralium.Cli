using System;
using Neuralia.Blockchains.Core.Configuration;
using Neuralia.Blockchains.Core.Services;

namespace Neuralium.Cli.Classes.Runtime {
	public class AppSettings {
		public bool UseTls { get; set; } = false;

		public int RpcPort { get; set; } = GlobalsService.DEFAULT_RPC_PORT;

		public string Host { get; set; } = "localhost";
		
		/// <summary>
		/// username for RPC authentication (if used by the node)
		/// </summary>
		public string User { get; set; }
	}

}