using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;
using System.IO;
using SIE.IO;

// 单字节类型：Int8(sbyte) / UInt8(byte) / Boolean 的读写基准
// 三路对比：BinaryWriter(BinaryReader) 基线 vs ByteArray vs Buffer<byte>

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class ByteBenchmark {
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;
    private ByteArray _byteArray;
    private Buffer<byte> _buffer;

    private const byte ByteValue = 0xAB;
    private const sbyte SByteValue = -85;
    private const bool BoolValue = true;
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
            _writer.Write(ByteValue);
            _writer.Write(SByteValue);
            _writer.Write(BoolValue);
            _byteArray.WriteInt8(ByteValue);
            _byteArray.WriteInt8(SByteValue);
            _byteArray.WriteBoolean(BoolValue);
            _buffer.WriteUInt8(ByteValue);
            _buffer.WriteInt8(SByteValue);
            _buffer.WriteBoolean(BoolValue);
        }
        _writer.Flush();
    }

    // ------------------
    // Write
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteUInt8")]
    public long BinaryWriter_WriteUInt8() {
        _stream.Position = 0;
        for (int i = 0; i < N; i++) _writer.Write(ByteValue);
        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteUInt8")]
    public long ByteArray_WriteUInt8() {
        _byteArray.Position = 0;
        for (int i = 0; i < N; i++) _byteArray.WriteInt8(ByteValue);
        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteUInt8")]
    public long Buffer_WriteUInt8() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteUInt8(ByteValue);
        return _buffer.Position;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteInt8")]
    public long BinaryWriter_WriteInt8() {
        _stream.Position = 0;
        for (int i = 0; i < N; i++) _writer.Write(SByteValue);
        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt8")]
    public long ByteArray_WriteInt8() {
        _byteArray.Position = 0;
        for (int i = 0; i < N; i++) _byteArray.WriteInt8(SByteValue);
        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt8")]
    public long Buffer_WriteInt8() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteInt8(SByteValue);
        return _buffer.Position;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteBoolean")]
    public long BinaryWriter_WriteBoolean() {
        _stream.Position = 0;
        for (int i = 0; i < N; i++) _writer.Write(BoolValue);
        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteBoolean")]
    public long ByteArray_WriteBoolean() {
        _byteArray.Position = 0;
        for (int i = 0; i < N; i++) _byteArray.WriteBoolean(BoolValue);
        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteBoolean")]
    public long Buffer_WriteBoolean() {
        _buffer.Position = 0;
        for (int i = 0; i < N; i++) _buffer.WriteBoolean(BoolValue);
        return _buffer.Position;
    }

    // ------------------
    // Read
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadUInt8")]
    public int BinaryReader_ReadUInt8() {
        _stream.Position = _readOffset;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _reader.ReadByte();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadUInt8")]
    public int ByteArray_ReadUInt8() {
        _byteArray.Position = _readOffset;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _byteArray.ReadUInt8();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadUInt8")]
    public int Buffer_ReadUInt8() {
        _buffer.Position = _readOffset;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _buffer.ReadUInt8();
        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadInt8")]
    public int BinaryReader_ReadInt8() {
        _stream.Position = _readOffset;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _reader.ReadSByte();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadInt8")]
    public int ByteArray_ReadInt8() {
        _byteArray.Position = _readOffset;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _byteArray.ReadInt8();
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadInt8")]
    public int Buffer_ReadInt8() {
        _buffer.Position = _readOffset;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _buffer.ReadInt8();
        return result;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadBoolean")]
    public int BinaryReader_ReadBoolean() {
        _stream.Position = _readOffset;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _reader.ReadBoolean() ? 1 : 0;
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadBoolean")]
    public int ByteArray_ReadBoolean() {
        _byteArray.Position = _readOffset;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _byteArray.ReadBoolean() ? 1 : 0;
        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadBoolean")]
    public int Buffer_ReadBoolean() {
        _buffer.Position = _readOffset;
        int result = 0;
        for (int i = 0; i < N; i++) result ^= _buffer.ReadBoolean() ? 1 : 0;
        return result;
    }
}
