#if !DEBUG
using BenchmarkDotNet.Running;

#endif

namespace Serialize.Benchmark {
	public class Program {
		public static void Main(string[] args) {
#if DEBUG
			DebugRunner.Run();
#else
            // BenchmarkRunner.Run(typeof(Program).Assembly);

            // BenchmarkRunner.Run<Int32Benchmark>();
            // BenchmarkRunner.Run<Int16Benchmark>();
            // BenchmarkRunner.Run<Int64Benchmark>();
            // BenchmarkRunner.Run<DoubleBenchmark>();
            // BenchmarkRunner.Run<VarInt32Benchmark>();
            // BenchmarkRunner.Run<VarInt64Benchmark>();
            BenchmarkRunner.Run<UTF8Benchmark>();
#endif
		}
	}
}
