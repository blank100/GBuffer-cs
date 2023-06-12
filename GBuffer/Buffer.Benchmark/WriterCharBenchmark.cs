using System.Collections.Generic;
using System.Text;

using BenchmarkDotNet.Attributes;

using Gal.Core;

namespace Serialize.Benchmark {
	[MemoryDiagnoser, RankColumn]
	public class WriterCharBenchmark {
		private int count = 1000;

		[Benchmark(Baseline = true)]
		public void WriterWriteString() {
			for (int j = 0; j < 100; j++) {
				using Writer<char> writer = new(1024);
				for (var i = 0; i < count; i++) {
					writer.Write("[]0123456");
				}
				string text = new(writer.writtenSpan);
			}
		}

		[Benchmark]
		public void ListStringJoin() {
			for (int j = 0; j < 100; j++) {
				List<string> writer = new(1024);
				for (var i = 0; i < count; i++) {
					writer.Add("[]0123456");
				}
				string text = string.Join(string.Empty, writer);
			}
		}

		[Benchmark]
		public void StringBuilderWrite() {
			for (int j = 0; j < 100; j++) {
				StringBuilder writer = new(1024);
				for (var i = 0; i < count; i++) {
					writer.Append("[]0123456");
				}
				string text = writer.ToString();
			}
		}
	}
}