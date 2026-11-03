using System.Diagnostics;

using Gal.Core;

namespace Serialize.Benchmark {
	public static class DebugRunner {
		/// <summary>
		/// Debug 模式下的快速自检：验证 Writer/Reader 基本读写并输出耗时（不依赖 BenchmarkDotNet）。
		/// </summary>
		public static void Run() {
			const int n = 1_000_000;

			var sw = Stopwatch.StartNew();
			using var writer = new Writer<byte>(n * 4);
			for (var i = 0; i < n; i++) writer.WriteInt32(i);
			sw.Stop();
			Console.WriteLine($"Writer<byte>.WriteInt32 x{n}: {sw.ElapsedMilliseconds} ms, {writer.Length:N0} bytes");

			var reader = new Reader<byte>(writer.WrittenSpan.ToArray());
			sw.Restart();
			long sum = 0;
			for (var i = 0; i < n; i++) sum += reader.ReadInt32();
			sw.Stop();
			Console.WriteLine($"Reader<byte>.ReadInt32  x{n}: {sw.ElapsedMilliseconds} ms, sum={sum:N0}");

			if (sum != (long)n * (n - 1) / 2) throw new InvalidOperationException("DebugRunner self-check failed.");
			Console.WriteLine("DebugRunner self-check passed.");
		}
	}
}