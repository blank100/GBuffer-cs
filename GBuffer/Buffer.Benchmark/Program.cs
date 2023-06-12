#if !DEBUG
using BenchmarkDotNet.Running;

#endif

namespace Serialize.Benchmark {
	public class Program {
		public static void Main(string[] args) {
#if DEBUG
			DebugRunner.Run();
#else
			BenchmarkRunner.Run<WriterCharBenchmark>();
#endif
		}
	}
}