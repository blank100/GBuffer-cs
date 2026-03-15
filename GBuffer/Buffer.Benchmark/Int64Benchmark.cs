using System.Buffers.Binary;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SIE.IO;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class Int64Benchmark {
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;

    private ByteArray _byteArray;

    private Buffer<byte> _buffer;

    private Buffer<byte> _buffer2;

    private const long Value = 123456;
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
        _buffer2 = new Buffer<byte>(size);

        _readOffset = 8192 * 2;

        _stream.Position = _readOffset;
        _byteArray.Length = size;
        _byteArray.Position = _readOffset;
        _buffer.Position = _readOffset;
        _buffer2.Position = _readOffset;

        for (int i = 0; i < MaxN; i++) {
            _writer.Write(Value);
            _byteArray.WriteInt64(Value);
            _buffer.WriteInt64(Value);
            _buffer2.WriteInt64(Value);
        }

        _writer.Flush();
    }

    // ------------------
    // Write
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public long BinaryWriter_WriteInt64() {
        _stream.Position = 0;

        for (int i = 0; i < N; i++)
            _writer.Write(Value);

        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteInt64() {
        _byteArray.Position = 0;

        for (int i = 0; i < N; i++)
            _byteArray.WriteInt64(Value);

        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteInt64() {
        _buffer.Position = 0;

        for (int i = 0; i < N; i++)
             _buffer.WriteInt64(Value);

        return _buffer.Position;
    }

    // ------------------
    // Read
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public long BinaryReader_ReadInt64() {
        _stream.Position = _readOffset;

        long result = 0;

        for (int i = 0; i < N; i++)
            result ^= _reader.ReadInt64();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public long ByteArray_ReadInt64() {
        _byteArray.Position = _readOffset;

        long result = 0;

        for (int i = 0; i < N; i++)
            result ^= _byteArray.ReadInt64();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public long Buffer_ReadInt64() {
        _buffer.Position = _readOffset;

        long result = 0;

        for (int i = 0; i < N; i++) result ^= _buffer.ReadInt64();

        return result;
    }
}
