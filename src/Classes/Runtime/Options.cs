using System.Collections.Generic;
using CommandLine;

namespace Neuralium.Cli.Classes.Runtime {
	public abstract class OptionsBase {
		[Option('h', "host", Default = "", Required = false, HelpText = "The host IP as 1.2.3.4 or [2001:db8::1].")]
		public string Host { get; set; }
		
		[Option('p',"port", Default = null, Required = false, HelpText = "The host port.")]
		public int? Port { get; set; }
		
		[Option('c', "config", Default = "", Required = false, HelpText = "The path to a json configuration file.")]
		public string ConfigurationFile { get; set; }

		[Option('r', "runtime-mode", Default = "", Required = false, HelpText = "Are we running this in docker or not.")]
		public string RuntimeMode { get; set; }
	}

	[Verb("int", HelpText = "Add file contents to the index.")]
	public class InteractiveOptions : OptionsBase {

	}

	[Verb("run", HelpText = "Add file contents to the index.")]
	public class RunOptions : OptionsBase {

		[Option('o', "operation", Default = "", Required = false, HelpText = "The RPC method name to call.")]
		public string Operation { get; set; }

		[Option("jparams", Default = "", Required = false, SetName = "params", HelpText = "The json set of parameters.")]
		public string JParameters { get; set; }

		[Option("params", Separator = ';', Required = false, SetName = "params", HelpText = "The sequential set of parameters.")]
		public IEnumerable<string> Parameters { get; set; }
		
	}
}