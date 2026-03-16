using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Gal.Core;
using SIE.IO;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 5, iterationCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class UTF8Benchmark {
    private ByteArray _byteArrayRead;
    private ByteArray _byteArrayWrite;

    private Buffer<byte> _bufferRead;
    private Buffer<byte> _bufferWrite;

    private readonly string[] _testValues = {
        "",                     // 1. 空字符串：测试最小开销/分支预测
        "a",                    // 2. 单字符：极小负载
        "12345678",             // 3. 8位纯数字：测试 8 字节对齐
        "Hello World!",         // 4. 纯 ASCII：测试快速拷贝路径
        "abc中文测试",           // 5. 中英混合：测试多字节编码逻辑
        "🔥🚀Unicode",           // 6. 表情符号：测试 4 字节 UTF-8 编码
        new string('x', 256),   // 7. 长字符串：测试缓冲区扩容与大内存拷贝
        "        "              // 8. 纯空格：测试特定的字符处理
    };

    // 对于 UTF8 字符串测试，10,000,000 次可能过大（单次迭代几秒钟）
    // 建议设为 1,000,000 以获得合理的测试时间
    private const int ITERATIONS = 2_000_000;

    [GlobalSetup]
    public void Setup() {
        // 动态计算单组数据的最大可能字节数
        long bytesPerGroup = 0;
        foreach (var v in _testValues) {
            // UTF8 编码下，每个字符最多 4 字节，外加 2 字节长度头
            bytesPerGroup += (v.Length * 4) + 2;
        }

        // 预留 20% 的安全余量，防止因对齐等原因导致的细微偏差
        long totalSize = (long)(ITERATIONS * bytesPerGroup * 1.2);

        // 如果 size 过大（超过 2GB），BenchmarkDotNet 可能会报错，需注意控制 ITERATIONS
        _byteArrayRead = new ByteArray((int)Math.Min(totalSize, int.MaxValue - 56));
        _byteArrayWrite = new ByteArray((int)Math.Min(totalSize, int.MaxValue - 56));

        _bufferRead = new Buffer<byte>((int)Math.Min(totalSize, int.MaxValue - 56));
        _bufferWrite = new Buffer<byte>((int)Math.Min(totalSize, int.MaxValue - 56));

        // 预填充读取数据
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var value in _testValues) {
                _byteArrayRead.WriteUTF8(value);
                _bufferRead.WriteUtf8(value);
            }
        }
    }

    [IterationSetup]
    public void IterationSetup() {
        _byteArrayRead.Position = 0;
        _byteArrayWrite.Position = 0;
        _bufferRead.Position = 0;
        _bufferWrite.Position = 0;
    }

    // ------------------
    // Write 基准
    // ------------------

    [Benchmark(Baseline = true), BenchmarkCategory("Write")]
    public long ByteArray_WriteUTF8() {
        // 提取到局部变量减少字段访问开销
        var values = _testValues;
        var writer = _byteArrayWrite;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) writer.WriteUTF8(v);
        }
        return writer.Position;
    }

    [Benchmark, BenchmarkCategory("Write")]
    public long Buffer_WriteUtf8() {
        var values = _testValues;
        var writer = _bufferWrite;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) writer.WriteUtf8(v);
        }
        return writer.Position;
    }

    // ------------------
    // Read 基准
    // ------------------

    [Benchmark(Baseline = true), BenchmarkCategory("Read")]
    public int ByteArray_ReadUTF8() {
        var reader = _byteArrayRead;
        reader.Position = 0;
        int result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                // 修复逻辑错误：原代码这里调用了 WriteUTF8()
                string s = reader.ReadUTF8();
                result ^= s.Length; // 累加长度防止死代码消除
            }
        }
        return result;
    }

    [Benchmark, BenchmarkCategory("Read")]
    public int Buffer_ReadUtf8() {
        var reader = _bufferRead;
        reader.Position = 0;
        int result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                // 假设 Buffer 对应的读取方法是 ReadUtf8()
                string s = reader.ReadUtf8();
                result ^= s.Length;
            }
        }
        return result;
    }
}
