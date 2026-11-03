using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;
using System.Buffers.Binary;

// bigEndian=true 分支的读写基准
// 对照基线：BinaryPrimitives 大端手写（Manual_BigEndian_*）
// Buffer 端：WriteXxx(value, bigEndian: true)；读端走 Span 静态 Read(span, bigEndian: true)
// （Buffer<byte> 的 ReadInt32 扩展没有 bigEndian 参数，大端读统一走 SpanByteUtils）

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class BigEndianBenchmark {
    private Buffer<byte> _buffer;
    private byte[] _manualWrite;
    private byte[] _spanRead;

    private const int Int32Value = 123456;
    private const long Int64Value = 1234567890123456789L;
    private const double DoubleValue = 3.141592653589793;
    private const string Utf8Value = "Hello, 大端世界! 🌍";
    private const int MaxN = 4096;

    [Params(1, 16, 128, 1024)] public int N;

    [GlobalSetup]
    public void Setup() {
        int size = 1024 * 1024;
        _buffer = new Buffer<byte>(size);
        _manualWrite = new byte[size];
        _spanRead = new byte[size];

        // 预填充读端数据（大端布局）
        var fill = new Span<byte>(_spanRead);
        int off = 0;
        for (int i = 0; i < MaxN; i++) {
            off += SpanByteUtils.WriteInt32(fill.Slice(off), Int32Value, bigEndian: true);
            off += SpanByteUtils.WriteInt64(fill.Slice(off), Int64Value, bigEndian: true);
            off += SpanByteUtils.WriteDouble(fill.Slice(off), DoubleValue, bigEndian: true);
            off += SpanByteUtils.WriteUtf8(fill.Slice(off), Utf8Value, bigEndian: true);
        }
    }

    // ------------------
    // Write (bigEndian)
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteInt32BE")]
    public int Manual_WriteInt32BE() {
        Span<byte> span = _manualWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset), Int32Value);
            offset += 4;
        }
        return offset;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt32BE")]
    public int Span_WriteInt32BE() {
        Span<byte> span = _manualWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            offset += SpanByteUtils.WriteInt32(span.Slice(offset), Int32Value, bigEndian: true);
        }
        return offset;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteInt64BE")]
    public int Manual_WriteInt64BE() {
        Span<byte> span = _manualWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            BinaryPrimitives.WriteInt64BigEndian(span.Slice(offset), Int64Value);
            offset += 8;
        }
        return offset;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt64BE")]
    public int Span_WriteInt64BE() {
        Span<byte> span = _manualWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            offset += SpanByteUtils.WriteInt64(span.Slice(offset), Int64Value, bigEndian: true);
        }
        return offset;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteDoubleBE")]
    public int Manual_WriteDoubleBE() {
        Span<byte> span = _manualWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            BinaryPrimitives.WriteInt64BigEndian(span.Slice(offset), BitConverter.DoubleToInt64Bits(DoubleValue));
            offset += 8;
        }
        return offset;
    }

    [Benchmark]
    [BenchmarkCategory("WriteDoubleBE")]
    public long Buffer_WriteDoubleBE() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++)
            _buffer.WriteDouble(DoubleValue, bigEndian: true);
        return _buffer.Position;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteUtf8BE")]
    public int Manual_WriteUtf8BE() {
        Span<byte> span = _manualWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            int len = System.Text.Encoding.UTF8.GetByteCount(Utf8Value);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), (ushort)len);
            offset += 2;
            System.Text.Encoding.UTF8.GetBytes(Utf8Value.AsSpan(), span.Slice(offset));
            offset += len;
        }
        return offset;
    }

    [Benchmark]
    [BenchmarkCategory("WriteUtf8BE")]
    public long Buffer_WriteUtf8BE() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++)
            _buffer.WriteUtf8(Utf8Value, bigEndian: true);
        return _buffer.Position;
    }

    // ------------------
    // Read (bigEndian，走 Span 静态路径)
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadInt32BE")]
    public int Manual_ReadInt32BE() {
        ReadOnlySpan<byte> span = _spanRead;
        int result = 0;
        for (int i = 0; i < N; i++) {
            result ^= BinaryPrimitives.ReadInt32BigEndian(span);
            span = span.Slice(4);
        }
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadInt32BE")]
    public int Span_ReadInt32BE() {
        ReadOnlySpan<byte> span = _spanRead;
        int result = 0;
        for (int i = 0; i < N; i++) {
            result ^= SpanByteUtils.ReadInt32(span, bigEndian: true);
            span = span.Slice(4);
        }
        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadDoubleBE")]
    public double Manual_ReadDoubleBE() {
        ReadOnlySpan<byte> span = _spanRead;
        double result = 0;
        for (int i = 0; i < N; i++) {
            result += BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(span));
            span = span.Slice(8);
        }
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadDoubleBE")]
    public double Span_ReadDoubleBE() {
        ReadOnlySpan<byte> span = _spanRead;
        double result = 0;
        for (int i = 0; i < N; i++) {
            result += SpanByteUtils.ReadDouble(span, bigEndian: true);
            span = span.Slice(8);
        }
        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadUtf8BE")]
    public int Manual_ReadUtf8BE() {
        ReadOnlySpan<byte> span = _spanRead;
        int result = 0;
        for (int i = 0; i < N; i++) {
            int len = BinaryPrimitives.ReadUInt16BigEndian(span);
            span = span.Slice(2);
            result += len;
            span = span.Slice(len);
        }
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadUtf8BE")]
    public int Span_ReadUtf8BE() {
        ReadOnlySpan<byte> span = _spanRead;
        int result = 0;
        for (int i = 0; i < N; i++) {
            var s = SpanByteUtils.ReadUtf8(span, out int readCount, bigEndian: true);
            result += s.Length + readCount;
            span = span.Slice(readCount);
        }
        return result;
    }
}
