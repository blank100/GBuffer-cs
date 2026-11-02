
using Gal.Core;
#if !DEBUG
using BenchmarkDotNet.Running;

#endif

namespace Serialize.Benchmark {
	public class Program {
		public static void Main(string[] args) {
#if DEBUG
			DebugRunner.Run();
#else
            // 支持命令行过滤，例如:
            //   dotnet Buffer.Benchmark.dll --filter *VarInt32*
            //   dotnet Buffer.Benchmark.dll --filter *Float* --job dry
            // 不带参数时跑全部 benchmark
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#endif
        }
	}
}
