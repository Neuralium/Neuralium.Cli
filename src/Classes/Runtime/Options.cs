using System.Collections.Generic;
using CommandLine;

namespace Neuralium.Cli.Classes.Runtime {
	[Verb("interactive", HelpText = "Add file contents to the index.")]
	public class InteractiveOptions {

	}

	[Verb("query", HelpText = "Add file contents to the index.")]
	public class QueryOptions {

		[Value(0)]
		public string Operation { get; set; }

		[Value(1)]
		public IEnumerable<string> Parameters { get; set; }
	}
}