using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using Gal.Core;
using SIE.IO;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class DoubleBenchmark
{
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;

    private ByteArray _byteArray;
    private Buffer<byte> _buffer;

    private const double Value = 1234.56;
    private const int MaxN = 4096;

    private int _readOffset;

    private Consumer _consumer;

    [Params(1, 16, 128, 1024)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        int size = 1024 * 1024;

        _stream = new MemoryStream(size);
        _writer = new BinaryWriter(_stream, System.Text.Encoding.UTF8, true);
        _reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, true);

        _byteArray = new ByteArray(size);
        _buffer = new Buffer<byte>(size);

        _consumer = new Consumer();

        _readOffset = 8192 * 2;

        _stream.Position = _readOffset;
        _byteArray.Length = size;
        _byteArray.Position = _readOffset;
        _buffer.Position = _readOffset;

        for (int i = 0; i < MaxN; i++)
        {
            _writer.Write(Value);
            _byteArray.WriteDouble(Value);
            _buffer.WriteDouble(Value);
        }

        _writer.Flush();
    }

    // ------------------
    // Write
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public void BinaryWriter_WriteDouble()
    {
        _stream.Position = 0;

        for (int i = 0; i < N; i++)
            _writer.Write(Value);

        _consumer.Consume(_stream.Position);
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public void ByteArray_WriteDouble()
    {
        _byteArray.Position = 0;

        for (int i = 0; i < N; i++)
            _byteArray.WriteDouble(Value);

        _consumer.Consume(_byteArray.Position);
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public void Buffer_WriteDouble()
    {
        _buffer.Position = 0;

        for (int i = 0; i < N; i++)
            _buffer.WriteDouble(Value);

        _consumer.Consume(_buffer.Position);
    }

    // ------------------
    // Read
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public void BinaryReader_ReadDouble()
    {
        _stream.Position = _readOffset;

        long result = 0;

        for (int i = 0; i < N; i++)
        {
            double v = _reader.ReadDouble();
            result ^= BitConverter.DoubleToInt64Bits(v);
        }

        _consumer.Consume(result);
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public void ByteArray_ReadDouble()
    {
        _byteArray.Position = _readOffset;

        long result = 0;

        for (int i = 0; i < N; i++)
        {
            double v = _byteArray.ReadDouble();
            result ^= BitConverter.DoubleToInt64Bits(v);
        }

        _consumer.Consume(result);
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public void Buffer_ReadDouble()
    {
        _buffer.Position = _readOffset;

        long result = 0;

        for (int i = 0; i < N; i++)
        {
            double v = _buffer.ReadDouble();
            result ^= BitConverter.DoubleToInt64Bits(v);
        }

        _consumer.Consume(result);
    }
}
