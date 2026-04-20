using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Gal.Core;
using System.IO;
using SIE.IO;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class Int32Benchmark {
    private MemoryStream _stream;
    private BinaryWriter _writer;
    private BinaryReader _reader;

    private ByteArray _byteArray;

    private Buffer2<byte> _buffer;

    private byte[] _SpanRead;
    private byte[] _SpanWrite;

    private const int Value = 123456;
    private const int MaxN = 4096;

    private int _readOffset;

    [Params(1, 16, 128, 1024)] public int N;

    [GlobalSetup]
    public unsafe void Setup() {
        int size = 1024 * 1024;

        _stream = new MemoryStream(size);
        _writer = new BinaryWriter(_stream, System.Text.Encoding.UTF8, true);
        _reader = new BinaryReader(_stream, System.Text.Encoding.UTF8, true);

        _byteArray = new ByteArray(size);

        _buffer = new Buffer2<byte>(size);

        _SpanRead = new byte[size];
        _SpanWrite = new byte[size];

        _readOffset = 4096;

        _stream.Position = _readOffset;
        _byteArray.Length = size;
        _byteArray.Position = _readOffset;
        _buffer.Position = _readOffset;

        for (int i = 0; i < MaxN; i++) {
            _writer.Write(Value);
            _byteArray.WriteInt32(Value);
            _buffer.WriteInt32(Value);
        }

        fixed (byte* ptr = _SpanRead) {
            var bytes = ptr;
            for (int i = 0; i < MaxN; i++) {
                BytesWriter.WriteInt32(ref bytes, Value);
            }
        }

        _writer.Flush();
    }

    // ------------------
    // Write
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public long BinaryWriter_WriteInt32() {
        _stream.Position = 0;

        for (int i = 0; i < N; i++)
            _writer.Write(Value);

        return _stream.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteInt32() {
        _byteArray.Position = 0;

        for (int i = 0; i < N; i++)
            _byteArray.WriteInt32(Value);

        return _byteArray.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteInt32() {
        _buffer.Position = 0;

        for (int i = 0; i < N; i++)
            _buffer.WriteInt32(Value);

        return _buffer.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public unsafe long Bytes_WriteInt32() {
        fixed (byte* ptr = _SpanWrite) {
            var bytes = ptr;
            for (int i = 0; i < N; i++) {
                BytesWriter.WriteInt32(ref bytes, Value);
            }

            return bytes - ptr;
        }
    }

    // ------------------
    // Read
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public int BinaryReader_ReadInt32() {
        _stream.Position = _readOffset;

        int result = 0;

        for (int i = 0; i < N; i++) result ^= _reader.ReadInt32();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public int ByteArray_ReadInt32() {
        _byteArray.Position = _readOffset;

        int result = 0;

        for (int i = 0; i < N; i++)
            result ^= _byteArray.ReadInt32();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public int Buffer_ReadInt32() {
        _buffer.Position = _readOffset;

        int result = 0;

        for (int i = 0; i < N; i++)
            result ^= _buffer.ReadInt32();

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public unsafe int Bytes_ReadInt32() {
        int result = 0;

        fixed (byte* ptr = _SpanRead) {
            var bytes = ptr;
            for (int i = 0; i < N; i++) {
                result ^= BytesReader.Read<int>(ref bytes);
            }
        }

        return result;
    }
}
