using System;
using System.Runtime.Serialization;

namespace Neuralium.Cli.Classes {
	public class NoLongRunningException : Exception {

		public NoLongRunningException() {
		}

		protected NoLongRunningException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

		public NoLongRunningException(string message) : base(message) {
		}

		public NoLongRunningException(string message, Exception innerException) : base(message, innerException) {
		}
	}
}