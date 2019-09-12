namespace Neuralium.Cli.Classes.Runtime {
	public class AppSettings {
		public bool UseTls { get; set; } = false;

		public int RpcPort { get; set; } = 12033;

		public string ServerDNS { get; set; } = "localhost";
	}

}