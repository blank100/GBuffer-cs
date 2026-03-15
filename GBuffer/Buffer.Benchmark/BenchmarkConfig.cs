using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;

public class BenchmarkConfig : ManualConfig {
    public BenchmarkConfig() {
        AddJob(Job.Default
            .WithWarmupCount(5) // 增加预热次数，确保 JIT 彻底完成
            .WithIterationCount(10) // 保持 10 轮正式迭代
            // --- 核心修复点 ---
            // 方式 A：强制单次迭代至少运行 250 毫秒（推荐，框架会自动计算调用次数）
            // .WithMinIterationTime(TimeInterval.FromMilliseconds(250))
            // 方式 B：如果方法极快，手动指定每轮迭代内部执行方法的次数（如 1000 次）
            // .WithInvocationCount(1000)
        );

        // 建议增加 Median（中位数）和 Ratio（对比倍数），对分析双峰很有帮助
        AddColumn(TargetMethodColumn.Method,
            StatisticColumn.Mean,
            StatisticColumn.Median,
            StatisticColumn.StdDev
        );
    }
}
