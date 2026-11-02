using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;
using System.IO;
using SIE.IO;

// 定长无符号整数 UInt16 / UInt32 / UInt64 的读写基准
// 三路对比：BinaryWriter(BinaryReader) 基线 vs ByteArray vs Buffer<byte>

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class UInt16Benchmark {
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;
    private ByteArray _byteArray;
    private Buffer<byte> _buffer;

    private const ushort Value = 54321;
    private const int MaxN = 4096;
    private int _readOffset;

    [Params(1, 16, 128, 1024)] public int N;

    [GlobalSetup]
    public void Setup() {
        int size = 1024 * 1024;
        _stream = new MemoryStream(size);
        _writer = new BinaryWriter(_stream, System.Text.Encoding.UTF8, true);
        _reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, true);
        _byteArray = new ByteArray(size);
        _buffer = new Buffer<byte>(size);

        _readOffset = 4096;
        _stream.Position = _readOffset;
        _byteArray.Length = size;
        _byteArray.Position = _readOffset;
        _buffer.Position = _readOffset;

        for (int i = 0; i < MaxN; i++) {
            _writer.Write(Value);
            _byteArray.WriteInt16(Value);
            _buffer.WriteUInt16(Value);
        }
        _writer.Flush();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public long BinaryWriter_WriteUInt16() {
        _stream.Position = 0;
        for (int i = 0; i < N; i++) _writer.Write(Value);
        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteUInt16() {
        _byteArray.Position = 0;
        for (int i = 0; i < N; i++) _byteArray.WriteInt16(Value);
        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteUInt16() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteUInt16(Value);
        return _buffer.Position;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public uint BinaryReader_ReadUInt16() {
        _stream.Position = _readOffset;
        uint result = 0;
        for (int i = 0; i < N; i++) result ^= _reader.ReadUInt16();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public uint ByteArray_ReadUInt16() {
        _byteArray.Position = _readOffset;
        uint result = 0;
        for (int i = 0; i < N; i++) result ^= _byteArray.ReadUInt16();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public uint Buffer_ReadUInt16() {
        _buffer.Position = _readOffset;
        uint result = 0;
        for (int i = 0; i < N; i++) result ^= _buffer.ReadUInt16();
        return result;
    }
}

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class UInt32Benchmark {
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;
    private ByteArray _byteArray;
    private Buffer<byte> _buffer;

    private const uint Value = 3456789012u;
    private const int MaxN = 4096;
    private int _readOffset;

    [Params(1, 16, 128, 1024)] public int N;

    [GlobalSetup]
    public void Setup() {
        int size = 1024 * 1024;
        _stream = new MemoryStream(size);
        _writer = new BinaryWriter(_stream, System.Text.Encoding.UTF8, true);
        _reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, true);
        _byteArray = new ByteArray(size);
        _buffer = new Buffer<byte>(size);

        _readOffset = 4096;
        _stream.Position = _readOffset;
        _byteArray.Length = size;
        _byteArray.Position = _readOffset;
        _buffer.Position = _readOffset;

        for (int i = 0; i < MaxN; i++) {
            _writer.Write(Value);
            _byteArray.WriteUInt32(Value);
            _buffer.WriteUInt32(Value);
        }
        _writer.Flush();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public long BinaryWriter_WriteUInt32() {
        _stream.Position = 0;
        for (int i = 0; i < N; i++) _writer.Write(Value);
        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteUInt32() {
        _byteArray.Position = 0;
        for (int i = 0; i < N; i++) _byteArray.WriteUInt32(Value);
        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteUInt32() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteUInt32(Value);
        return _buffer.Position;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public uint BinaryReader_ReadUInt32() {
        _stream.Position = _readOffset;
        uint result = 0;
        for (int i = 0; i < N; i++) result ^= _reader.ReadUInt32();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public uint ByteArray_ReadUInt32() {
        _byteArray.Position = _readOffset;
        uint result = 0;
        for (int i = 0; i < N; i++) result ^= _byteArray.ReadUInt32();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public uint Buffer_ReadUInt32() {
        _buffer.Position = _readOffset;
        uint result = 0;
        for (int i = 0; i < N; i++) result ^= _buffer.ReadUInt32();
        return result;
    }
}

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class UInt64Benchmark {
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;
    private ByteArray _byteArray;
    private Buffer<byte> _buffer;

    private const ulong Value = 12345678901234567890ul;
    private const int MaxN = 4096;
    private int _readOffset;

    [Params(1, 16, 128, 1024)] public int N;

    [GlobalSetup]
    public void Setup() {
        int size = 1024 * 1024;
        _stream = new MemoryStream(size);
        _writer = new BinaryWriter(_stream, System.Text.Encoding.UTF8, true);
        _reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, true);
        _byteArray = new ByteArray(size);
        _buffer = new Buffer<byte>(size);

        _readOffset = 4096;
        _stream.Position = _readOffset;
        _byteArray.Length = size;
        _byteArray.Position = _readOffset;
        _buffer.Position = _readOffset;

        for (int i = 0; i < MaxN; i++) {
            _writer.Write(Value);
            _byteArray.WriteUInt64(Value);
            _buffer.WriteUInt64(Value);
        }
        _writer.Flush();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public long BinaryWriter_WriteUInt64() {
        _stream.Position = 0;
        for (int i = 0; i < N; i++) _writer.Write(Value);
        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteUInt64() {
        _byteArray.Position = 0;
        for (int i = 0; i < N; i++) _byteArray.WriteUInt64(Value);
        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteUInt64() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteUInt64(Value);
        return _buffer.Position;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public ulong BinaryReader_ReadUInt64() {
        _stream.Position = _readOffset;
        ulong result = 0;
        for (int i = 0; i < N; i++) result ^= _reader.ReadUInt64();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public ulong ByteArray_ReadUInt64() {
        _byteArray.Position = _readOffset;
        ulong result = 0;
        for (int i = 0; i < N; i++) result ^= _byteArray.ReadUInt64();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public ulong Buffer_ReadUInt64() {
        _buffer.Position = _readOffset;
        ulong result = 0;
        for (int i = 0; i < N; i++) result ^= _buffer.ReadUInt64();
        return result;
    }
}
