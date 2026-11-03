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
        // 注意：数值类型与 utf8 必须各自独占一块连续区域。
        // 早期版本把 int32/int64/double/utf8 交错写在同一缓冲区、读端却一律从 offset 0 起按 utf8 解析，
        // 结果读到的"长度前缀"其实是 int32 的高半字（1），随后稳定读出 16401 这样的垃圾长度，
        // 导致 Span_ReadUtf8BE 把 16 KB 二进制当 utf8 解码，产出 369x 的假异常值。
        // 现在数值区在前、utf8 区独立填充，读端只读自己那块。
        var fill = new Span<byte>(_spanRead);
        int off = 0;
        for (int i = 0; i < MaxN; i++) {
            off += SpanByteUtils.WriteInt32(fill.Slice(off), Int32Value, bigEndian: true);
            off += SpanByteUtils.WriteInt64(fill.Slice(off), Int64Value, bigEndian: true);
            off += SpanByteUtils.WriteDouble(fill.Slice(off), DoubleValue, bigEndian: true);
        }

        int utf8Off = 0;
        for (int i = 0; i < MaxN; i++) {
            utf8Off += SpanByteUtils.WriteUtf8(fill.Slice(utf8Off), Utf8Value, bigEndian: true);
        }

        // 自检：utf8 区第一条记录的长度前缀必须等于真实字节数，否则基准本身不可信
        var first = SpanByteUtils.ReadUInt16(fill.Slice(0), bigEndian: true);
        if (first != System.Text.Encoding.UTF8.GetByteCount(Utf8Value))
            throw new InvalidOperationException($"BigEndianBenchmark 读端数据布局错误：首条 utf8 长度前缀为 {first}，期望 {System.Text.Encoding.UTF8.GetByteCount(Utf8Value)}");
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

    // 基线：手写大端长度前缀 + Encoding.UTF8.GetString，与 Span_ReadUtf8BE 做同样的完整解码工作
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadUtf8BE")]
    public int Manual_ReadUtf8BE() {
        ReadOnlySpan<byte> span = _spanRead;
        int result = 0;
        for (int i = 0; i < N; i++) {
            int len = BinaryPrimitives.ReadUInt16BigEndian(span);
            result += System.Text.Encoding.UTF8.GetString(span.Slice(2, len)).Length;
            span = span.Slice(2 + len);
        }
        return result;
    }

    // 仅解析长度前缀（不解码），用来说明解码成本占比
    [Benchmark]
    [BenchmarkCategory("ReadUtf8BE")]
    public int Manual_ReadUtf8BE_LengthOnly() {
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
