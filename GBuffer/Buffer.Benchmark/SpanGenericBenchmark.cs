using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;

// Span 静态读写路径 + 泛型 IWriter<T>/Reader<T> 路径的基准
// - Span 静态：SpanByteUtils.WriteInt32(span)/WriteFloat(span)/WriteVarInt32(span)/WriteUtf8(span) 及对应 Read(span)
// - 泛型：Writer<byte>.Write(T)、Write(e1..e6) 多参重载、Write(ReadOnlySpan<byte>) 批量、Reader<byte>.Read()

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class SpanWriteBenchmark {
    private byte[] _spanWrite;
    private byte[] _spanReadData;
    private Buffer<byte> _buffer;

    private const int Int32Value = 123456;
    private const float FloatValue = 3.14159f;
    private const int VarIntValue = 987654321;
    private const string Utf8Value = "Span静态路径测试 Hello";
    private const int MaxN = 4096;

    [Params(1, 16, 128, 1024)] public int N;

    [GlobalSetup]
    public void Setup() {
        int size = 1024 * 1024;
        _spanWrite = new byte[size];
        _buffer = new Buffer<byte>(size);

        // 预填充读端数据：前段 MaxN 个 Int32，后段 MaxN 个 VarInt32（分段避免读串段）
        for (int i = 0; i < MaxN; i++) _buffer.WriteInt32(Int32Value);
        for (int i = 0; i < MaxN; i++) _buffer.WriteVarInt32(VarIntValue);
        _spanReadData = _buffer.WrittenSpan.ToArray();
    }

    // ------------------
    // Span 静态 Write
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SpanWriteInt32")]
    public int Buffer_WriteInt32() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteInt32(Int32Value);
        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("SpanWriteInt32")]
    public int Span_WriteInt32() {
        Span<byte> span = _spanWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            offset += SpanByteUtils.WriteInt32(span.Slice(offset), Int32Value);
        }
        return offset;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SpanWriteFloat")]
    public int Buffer_WriteFloat() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteFloat(FloatValue);
        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("SpanWriteFloat")]
    public int Span_WriteFloat() {
        Span<byte> span = _spanWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            offset += SpanByteUtils.WriteFloat(span.Slice(offset), FloatValue);
        }
        return offset;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SpanWriteVarInt32")]
    public int Buffer_WriteVarInt32() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteVarInt32(VarIntValue);
        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("SpanWriteVarInt32")]
    public int Span_WriteVarInt32() {
        Span<byte> span = _spanWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            offset += SpanByteUtils.WriteVarInt32(span.Slice(offset), VarIntValue);
        }
        return offset;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SpanWriteUtf8")]
    public int Buffer_WriteUtf8() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteUtf8(Utf8Value);
        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("SpanWriteUtf8")]
    public int Span_WriteUtf8() {
        Span<byte> span = _spanWrite;
        int offset = 0;
        for (int i = 0; i < N; i++) {
            offset += SpanByteUtils.WriteUtf8(span.Slice(offset), Utf8Value);
        }
        return offset;
    }

    // ------------------
    // Span 静态 Read
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SpanReadInt32")]
    public int Buffer_ReadInt32() {
        _buffer.Position = 0;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _buffer.ReadInt32();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("SpanReadInt32")]
    public int Span_ReadInt32() {
        ReadOnlySpan<byte> span = _spanReadData;
        int result = 0;
        for (int i = 0; i < N; i++) {
            result ^= SpanByteUtils.ReadInt32(span);
            span = span.Slice(4);
        }
        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("SpanReadVarInt32")]
    public int Buffer_ReadVarInt32() {
        _buffer.Position = MaxN * 4;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _buffer.ReadVarInt32();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("SpanReadVarInt32")]
    public int Span_ReadVarInt32() {
        ReadOnlySpan<byte> span = _spanReadData.AsSpan(MaxN * 4);
        int result = 0;
        for (int i = 0; i < N; i++) {
            result ^= SpanByteUtils.ReadVarInt32(span, out int readCount);
            span = span.Slice(readCount);
        }
        return result;
    }
}

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class GenericWriteBenchmark {
    private Writer<byte> _writer;
    private Buffer<byte> _buffer;
    private byte[] _batch;
    private byte[] _readData;
    private const int MaxN = 4096;

    [Params(1, 16, 128, 1024)] public int N;

    [GlobalSetup]
    public void Setup() {
        int size = 1024 * 1024;
        _writer = new Writer<byte>(size);
        _buffer = new Buffer<byte>(size);
        _batch = new byte[64];
        for (int i = 0; i < MaxN; i++) {
            _buffer.WriteUInt8((byte)i);
        }
        _readData = _buffer.WrittenSpan.ToArray();
    }

    // 泛型 Write(T) 单元素
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("GenericWrite")]
    public long Buffer_WriteByte() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteUInt8((byte)i);
        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("GenericWrite")]
    public int Writer_WriteElement() {
        _writer.Clear();
        for (int i = 0; i < N; i++) _writer.Write((byte)i);
        return _writer.Position;
    }

    // 多参重载 Write(e1..e6)
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("GenericWrite6")]
    public long Buffer_Write6Bytes() {
        _buffer.Position = 0;
        for (int i = 0; i < N / 6; i++)
            _buffer.Write((byte)i, (byte)i, (byte)i, (byte)i, (byte)i, (byte)i);
        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("GenericWrite6")]
    public int Writer_Write6Elements() {
        _writer.Clear();
        for (int i = 0; i < N / 6; i++)
            _writer.Write((byte)i, (byte)i, (byte)i, (byte)i, (byte)i, (byte)i);
        return _writer.Position;
    }

    // 批量 Write(ReadOnlySpan<byte>)
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("GenericWriteSpan")]
    public long Buffer_WriteSpan() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i += _batch.Length)
            _buffer.Write((ReadOnlySpan<byte>)_batch);
        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("GenericWriteSpan")]
    public int Writer_WriteSpan() {
        _writer.Clear();
        for (int i = 0; i < N; i += _batch.Length)
            _writer.Write((ReadOnlySpan<byte>)_batch);
        return _writer.Position;
    }

    // 泛型 Reader<T>.Read()
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("GenericRead")]
    public int Buffer_ReadByte() {
        _buffer.Position = 0;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _buffer.ReadUInt8();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("GenericRead")]
    public int Reader_ReadElement() {
        var reader = new Reader<byte>(_readData);
        int result = 0;
        for (int i = 0; i < N; i++) result ^= reader.Read();
        return result;
    }
}
