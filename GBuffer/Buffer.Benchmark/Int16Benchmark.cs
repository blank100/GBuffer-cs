using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;
using System.IO;
using SIE.IO;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class Int16Benchmark {
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;

    private ByteArray _byteArray;

    private Buffer<byte> _buffer;

    private const short Value = 12345;
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
            _buffer.WriteInt16(Value);
        }

        _writer.Flush();
    }

    // ------------------
    // Write
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public long BinaryWriter_WriteInt16() {
        _stream.Position = 0;

        for (int i = 0; i < N; i++)
            _writer.Write(Value);

        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteInt16() {
        _byteArray.Position = 0;

        for (int i = 0; i < N; i++)
            _byteArray.WriteInt16(Value);

        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteInt16() {
        _buffer.Position = 0;

        for (int i = 0; i < N; i++)
            _buffer.WriteInt16(Value);

        return _buffer.Position;
    }

    // ------------------
    // Read
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public int BinaryReader_ReadInt16() {
        _stream.Position = _readOffset;

        int result = 0;

        for (int i = 0; i < N; i++)
            result ^= _reader.ReadInt16();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public int ByteArray_ReadInt16() {
        _byteArray.Position = _readOffset;

        int result = 0;

        for (int i = 0; i < N; i++)
            result ^= _byteArray.ReadInt16();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public int Buffer_ReadInt16() {
        _buffer.Position = _readOffset;

        int result = 0;

        for (int i = 0; i < N; i++)
            result ^= _buffer.ReadInt16();

        return result;
    }
}
