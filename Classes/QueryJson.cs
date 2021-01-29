using System;
using System.Collections.Generic;
using System.Linq;

namespace Neuralium.Cli.Classes {
	public interface IQueryJson {
		string Operation { get; set; }

		int ParameterCount { get; }
		IEnumerable<string> FormattedParameters { get; }
	}

	public abstract class QueryJson<P> : IQueryJson
		where P : QueryJson<P>.OperationParameters {
		public List<P> Parameters { get; set; } = new List<P>();

		public string Operation { get; set; }

		public abstract int ParameterCount { get; }
		public abstract IEnumerable<string> FormattedParameters { get; }

		public class OperationParameters {

			public string Value { get; set; }
		}
	}

	public class QueryJsonNamed : QueryJson<QueryJsonNamed.NamedOperationParameters> {

		public override IEnumerable<string> FormattedParameters => this.Parameters.Select(e => e.ToString());
		public override int ParameterCount => this.Parameters.Count;

		public class NamedOperationParameters : OperationParameters {
			public string Name { get; set; }

			public override string ToString() {
				return $"{this.Name}-{this.Value}";
			}
		}
	}

	public class QueryJsonIndexed : QueryJson<QueryJsonIndexed.IndexedOperationParameters> {

		public override IEnumerable<string> FormattedParameters => this.Parameters.OrderBy(e => e.Index).Select(e => e.Element.ToString());

		public override int ParameterCount => this.Parameters.Count;

		public class IndexedOperationParameters : OperationParameters {

			public IndexedOperationParameters(int index, string value) {
				this.Index = index;
				this.Value = value;

				string[] entries = this.Value.Split("=", 2, StringSplitOptions.RemoveEmptyEntries);

				if(entries.Length == 1) {
					this.Element = entries[0].Trim();
					this.HasName = false;
				} else if(entries.Length == 2) {
					this.Name = entries[0].Trim();
					this.Element = entries[1].Trim();
					this.HasName = true;
				}
			}

			public string Name { get; }
			public string Element { get; }
			public bool HasName { get; }

			public int Index { get; }

			public override string ToString() {
				return $"{this.Index}-{this.Value}";
			}
		}
	}

}