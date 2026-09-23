using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;

public class BenchmarkConfig : ManualConfig {
    public BenchmarkConfig() {
        AddJob(Job.Default
            .WithWarmupCount(10)
            .WithIterationCount(15)
            // 让 BenchmarkDotNet 自动提高调用次数，避免极短迭代受到计时和 JIT 噪声影响。
            .WithMinIterationTime(TimeInterval.FromMilliseconds(100))
            // 微基准固定使用优化后的机器码，避免 Tier0 到 Tier1 的过渡混入结果。
            .WithEnvironmentVariable("DOTNET_TieredCompilation", "0")
            .WithEnvironmentVariable("DOTNET_TieredPGO", "0")
        );

        // 中位数和标准差比均值更能识别 JIT、GC 或频率变化造成的双峰结果。
        AddColumn(TargetMethodColumn.Method,
            StatisticColumn.Mean,
            StatisticColumn.Median,
            StatisticColumn.StdDev
        );
    }
}
