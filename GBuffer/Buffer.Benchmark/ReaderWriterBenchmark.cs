using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;
using System.IO;
using System.Text;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class ReaderWriterBenchmark {
    private MemoryStream _stream;
    private BinaryWriter _binaryWriter;
    private BinaryReader _binaryReader;

    private Writer<byte> _writer;
    private Reader<byte> _reader;
    private Buffer<byte> _buffer;

    private byte[] _readBytes;
    private byte[] _writeBytes;

    private const int Int32Value = 123456;
    private const long Int64Value = 1234567890123L;
    private const double DoubleValue = 1234.5678;

    private const int MaxN = 4096;

    private int _readOffsetInt32;
    private int _readOffsetInt64;
    private int _readOffsetDouble;

    [Params(1, 16, 128, 1024)] public int N;

    [GlobalSetup]
    public void Setup() {
        const int size = 1024 * 1024;

        _stream = new MemoryStream(size);
        _binaryWriter = new BinaryWriter(_stream, Encoding.UTF8, true);
        _binaryReader = new BinaryReader(_stream, Encoding.UTF8, true);

        _writer = new Writer<byte>(size);
        _buffer = new Buffer<byte>(size);

        _readBytes = new byte[size];
        _writeBytes = new byte[size];

        int block32 = MaxN * 4;
        int block64 = MaxN * 8;

        _readOffsetInt32 = 4096;
        _readOffsetInt64 = block32 + 4096;
        _readOffsetDouble = block32 + block64 + 4096;

        for (int i = 0; i < MaxN; i++) {
            _binaryWriter.Write(Int32Value);
            _writer.WriteInt32(Int32Value);
            _buffer.WriteInt32(Int32Value);
        }

        for (int i = 0; i < MaxN; i++) {
            _binaryWriter.Write(Int64Value);
            _writer.WriteInt64(Int64Value);
            _buffer.WriteInt64(Int64Value);
        }

        for (int i = 0; i < MaxN; i++) {
            _binaryWriter.Write(DoubleValue);
            _writer.WriteDouble(DoubleValue);
            _buffer.WriteDouble(DoubleValue);
        }

        _binaryWriter.Flush();

        _buffer.WrittenSpan.CopyTo(_readBytes);
        _reader = new Reader<byte>(_readBytes);
    }

    [GlobalCleanup]
    public void Cleanup() {
        _binaryWriter.Dispose();
        _binaryReader.Dispose();
        _stream.Dispose();
        _writer.Dispose();
        _buffer.Dispose();
    }

    // ------------------
    // Write Int32
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteInt32")]
    public long BinaryWriter_WriteInt32() {
        _stream.Position = 0;

        for (int i = 0; i < N; i++)
            _binaryWriter.Write(Int32Value);

        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt32")]
    public long Writer_WriteInt32() {
        _writer.Position = 0;

        for (int i = 0; i < N; i++)
            _writer.WriteInt32(Int32Value);

        return _writer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt32")]
    public long Buffer_WriteInt32() {
        _buffer.Position = 0;

        for (int i = 0; i < N; i++)
            _buffer.WriteInt32(Int32Value);

        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt32")]
    public long RefWriter_WriteInt32() {
        var writer = new RefWriter<byte>(_writeBytes);

        for (int i = 0; i < N; i++)
            writer.WriteInt32(Int32Value);

        return writer.Position;
    }

    // ------------------
    // Read Int32
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadInt32")]
    public int BinaryReader_ReadInt32() {
        _stream.Position = _readOffsetInt32;

        int result = 0;
        for (int i = 0; i < N; i++)
            result ^= _binaryReader.ReadInt32();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadInt32")]
    public int Reader_ReadInt32() {
        _reader.Position = _readOffsetInt32;

        int result = 0;
        for (int i = 0; i < N; i++)
            result ^= _reader.ReadInt32();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadInt32")]
    public int Buffer_ReadInt32() {
        _buffer.Position = _readOffsetInt32;

        int result = 0;
        for (int i = 0; i < N; i++)
            result ^= _buffer.ReadInt32();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadInt32")]
    public int RefReader_ReadInt32() {
        var reader = new RefReader<byte>(_readBytes) { Position = _readOffsetInt32 };

        int result = 0;
        for (int i = 0; i < N; i++)
            result ^= reader.ReadInt32();

        return result;
    }

    // ------------------
    // Write Int64
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteInt64")]
    public long BinaryWriter_WriteInt64() {
        _stream.Position = 0;

        for (int i = 0; i < N; i++)
            _binaryWriter.Write(Int64Value);

        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt64")]
    public long Writer_WriteInt64() {
        _writer.Position = 0;

        for (int i = 0; i < N; i++)
            _writer.WriteInt64(Int64Value);

        return _writer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt64")]
    public long Buffer_WriteInt64() {
        _buffer.Position = 0;

        for (int i = 0; i < N; i++)
            _buffer.WriteInt64(Int64Value);

        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteInt64")]
    public long RefWriter_WriteInt64() {
        var writer = new RefWriter<byte>(_writeBytes);

        for (int i = 0; i < N; i++)
            writer.WriteInt64(Int64Value);

        return writer.Position;
    }

    // ------------------
    // Read Int64
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadInt64")]
    public long BinaryReader_ReadInt64() {
        _stream.Position = _readOffsetInt64;

        long result = 0;
        for (int i = 0; i < N; i++)
            result ^= _binaryReader.ReadInt64();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadInt64")]
    public long Reader_ReadInt64() {
        _reader.Position = _readOffsetInt64;

        long result = 0;
        for (int i = 0; i < N; i++)
            result ^= _reader.ReadInt64();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadInt64")]
    public long Buffer_ReadInt64() {
        _buffer.Position = _readOffsetInt64;

        long result = 0;
        for (int i = 0; i < N; i++)
            result ^= _buffer.ReadInt64();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadInt64")]
    public long RefReader_ReadInt64() {
        var reader = new RefReader<byte>(_readBytes) { Position = _readOffsetInt64 };

        long result = 0;
        for (int i = 0; i < N; i++)
            result ^= reader.ReadInt64();

        return result;
    }

    // ------------------
    // Write Double
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("WriteDouble")]
    public long BinaryWriter_WriteDouble() {
        _stream.Position = 0;

        for (int i = 0; i < N; i++)
            _binaryWriter.Write(DoubleValue);

        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteDouble")]
    public long Writer_WriteDouble() {
        _writer.Position = 0;

        for (int i = 0; i < N; i++)
            _writer.WriteDouble(DoubleValue);

        return _writer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteDouble")]
    public long Buffer_WriteDouble() {
        _buffer.Position = 0;

        for (int i = 0; i < N; i++)
            _buffer.WriteDouble(DoubleValue);

        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("WriteDouble")]
    public long RefWriter_WriteDouble() {
        var writer = new RefWriter<byte>(_writeBytes);

        for (int i = 0; i < N; i++)
            writer.WriteDouble(DoubleValue);

        return writer.Position;
    }

    // ------------------
    // Read Double
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReadDouble")]
    public long BinaryReader_ReadDouble() {
        _stream.Position = _readOffsetDouble;

        long result = 0;
        for (int i = 0; i < N; i++)
            result ^= BitConverter.DoubleToInt64Bits(_binaryReader.ReadDouble());

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadDouble")]
    public long Reader_ReadDouble() {
        _reader.Position = _readOffsetDouble;

        long result = 0;
        for (int i = 0; i < N; i++)
            result ^= BitConverter.DoubleToInt64Bits(_reader.ReadDouble());

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadDouble")]
    public long Buffer_ReadDouble() {
        _buffer.Position = _readOffsetDouble;

        long result = 0;
        for (int i = 0; i < N; i++)
            result ^= BitConverter.DoubleToInt64Bits(_buffer.ReadDouble());

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("ReadDouble")]
    public long RefReader_ReadDouble() {
        var reader = new RefReader<byte>(_readBytes) { Position = _readOffsetDouble };

        long result = 0;
        for (int i = 0; i < N; i++)
            result ^= BitConverter.DoubleToInt64Bits(reader.ReadDouble());

        return result;
    }
}
