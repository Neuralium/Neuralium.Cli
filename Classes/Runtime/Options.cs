using Neuralia.NClap.Metadata;

namespace Neuralium.Cli.Classes.Runtime {
	public  class OptionsBase {
		[NamedArgument(ArgumentFlags.Optional, ShortName = "h", LongName = "host", DefaultValue = "", Description = "The host IP as 1.2.3.4 or [2001:db8::1].")]
		public string Host { get; set; }

		[NamedArgument(ArgumentFlags.Optional, ShortName = "p", LongName = "port", DefaultValue = null, Description = "The host port.")]
		public int? Port { get; set; }

		[NamedArgument(ArgumentFlags.Optional, ShortName = "c", LongName = "config", DefaultValue = "", Description = "The path to a json configuration file.")]
		public string ConfigurationFile { get; set; }

		[NamedArgument(ArgumentFlags.Optional, ShortName = "r", LongName = "runtime-mode", DefaultValue = "", Description = "Are we running this in docker or not.")]
		public string RuntimeMode { get; set; }
		
		[NamedArgument(ArgumentFlags.Optional, ShortName = "u", LongName = "user", DefaultValue = "", Description = "The username, only necessary if  'RpcAuthentication':'Basic' is used on the node")]
		public string User{get; set; }
		
		[PositionalArgument(ArgumentFlags.Optional, Position = 0)]
		public CommandGroup<ApiCommands> ApiCommand { get; set; }
		
	}
}