using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;
using System.IO;
using SIE.IO;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class FloatBenchmark {
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;

    private ByteArray _byteArray;

    private Buffer<byte> _buffer;

    private const float Value = 3.14159f;
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
            // ByteArray 没有 WriteFloat，用位模式走 Int32 路模拟
            _byteArray.WriteInt32(BitConverter.SingleToInt32Bits(Value));
            _buffer.WriteFloat(Value);
        }

        _writer.Flush();
    }

    // ------------------
    // Write
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public long BinaryWriter_WriteFloat() {
        _stream.Position = 0;

        for (int i = 0; i < N; i++)
            _writer.Write(Value);

        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteFloat() {
        _byteArray.Position = 0;

        for (int i = 0; i < N; i++)
            _byteArray.WriteInt32(BitConverter.SingleToInt32Bits(Value));

        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteFloat() {
        _buffer.Position = 0;

        for (int i = 0; i < N; i++)
            _buffer.WriteFloat(Value);

        return _buffer.Position;
    }

    // ------------------
    // Read
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public float BinaryReader_ReadFloat() {
        _stream.Position = _readOffset;

        float result = 0;

        for (int i = 0; i < N; i++) result += _reader.ReadSingle();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public float ByteArray_ReadFloat() {
        _byteArray.Position = _readOffset;

        float result = 0;

        for (int i = 0; i < N; i++)
            result += BitConverter.Int32BitsToSingle(_byteArray.ReadInt32());

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public float Buffer_ReadFloat() {
        _buffer.Position = _readOffset;

        float result = 0;

        for (int i = 0; i < N; i++)
            result += _buffer.ReadFloat();

        return result;
    }
}
