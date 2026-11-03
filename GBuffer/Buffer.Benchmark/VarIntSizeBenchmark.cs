using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;

// VarInt 写入前的"预计算长度"基准：GetVarUInt32Size / GetVarInt32Size / GetVarUInt64Size / GetVarInt64Size
// 典型用法：先算 size 分配精确 buffer，再写入。基线为手写 ULEB128 size 计算。

[Config(typeof(BenchmarkConfig))]
public class VarIntSizeBenchmark {
    // 覆盖 1~10 字节边界的典型值
    private static readonly uint[] UInt32Values = {
        0u, 1u, 127u, 128u, 16383u, 16384u, 2097151u, 2097152u,
        268435455u, 268435456u, uint.MaxValue
    };
    private static readonly int[] Int32Values = {
        0, 1, -1, 63, 64, -64, -65, 8191, 8192, -8192, -8193,
        1048575, 1048576, -1048576, int.MaxValue, int.MinValue
    };
    private static readonly ulong[] UInt64Values = {
        0ul, 1ul, 127ul, 128ul, 16383ul, 16384ul, 2097151ul, 2097152ul,
        268435455ul, 268435456ul, 34359738367ul, 34359738368ul,
        4398046511103ul, 4398046511104ul, 562949953421311ul, 562949953421312ul,
        72057594037927935ul, 72057594037927936ul, ulong.MaxValue
    };
    private static readonly long[] Int64Values = {
        0L, 1L, -1L, 63L, 64L, -64L, -65L, long.MaxValue, long.MinValue
    };

    private const int Iterations = 100000;

    // 手写 ULEB128 size 计算（基线）
    private static int ManualVarUInt32Size(uint value) {
        int size = 1;
        while (value >= 0x80) {
            value >>= 7;
            size++;
        }
        return size;
    }

    private static int ManualVarUInt64Size(ulong value) {
        int size = 1;
        while (value >= 0x80) {
            value >>= 7;
            size++;
        }
        return size;
    }

    private static int ManualZigZag32(int v) => (v << 1) ^ (v >> 31);
    private static long ManualZigZag64(long v) => (v << 1) ^ (v >> 63);

    [Benchmark(Baseline = true)]
    public int Manual_GetVarUInt32Size() {
        int total = 0;
        for (int i = 0; i < Iterations; i++)
            foreach (var v in UInt32Values)
                total += ManualVarUInt32Size(v);
        return total;
    }

    [Benchmark]
    public int Buffer_GetVarUInt32Size() {
        int total = 0;
        for (int i = 0; i < Iterations; i++)
            foreach (var v in UInt32Values)
                total += SpanByteUtils.GetVarUInt32Size(v);
        return total;
    }

    [Benchmark]
    public int Manual_GetVarInt32Size() {
        int total = 0;
        for (int i = 0; i < Iterations; i++)
            foreach (var v in Int32Values)
                total += ManualVarUInt32Size((uint)ManualZigZag32(v));
        return total;
    }

    [Benchmark]
    public int Buffer_GetVarInt32Size() {
        int total = 0;
        for (int i = 0; i < Iterations; i++)
            foreach (var v in Int32Values)
                total += SpanByteUtils.GetVarInt32Size(v);
        return total;
    }

    [Benchmark]
    public long Manual_GetVarUInt64Size() {
        long total = 0;
        for (int i = 0; i < Iterations; i++)
            foreach (var v in UInt64Values)
                total += ManualVarUInt64Size(v);
        return total;
    }

    [Benchmark]
    public long Buffer_GetVarUInt64Size() {
        long total = 0;
        for (int i = 0; i < Iterations; i++)
            foreach (var v in UInt64Values)
                total += SpanByteUtils.GetVarUInt64Size(v);
        return total;
    }

    [Benchmark]
    public long Manual_GetVarInt64Size() {
        long total = 0;
        for (int i = 0; i < Iterations; i++)
            foreach (var v in Int64Values)
                total += ManualVarUInt64Size((ulong)ManualZigZag64(v));
        return total;
    }

    [Benchmark]
    public long Buffer_GetVarInt64Size() {
        long total = 0;
        for (int i = 0; i < Iterations; i++)
            foreach (var v in Int64Values)
                total += SpanByteUtils.GetVarInt64Size(v);
        return total;
    }
}
