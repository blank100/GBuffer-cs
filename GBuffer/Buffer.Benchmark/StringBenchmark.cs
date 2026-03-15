using BenchmarkDotNet.Attributes;
using Gal.Core;

namespace Serialize.Benchmark;

[MemoryDiagnoser]
public class StringBenchmark
{
    private MemoryStream stream;
    private BinaryWriter binaryWriter;
    private BinaryReader binaryReader;

    private Buffer<byte> buffer;

    private byte[] bufferData;

    // Small / Medium / Large
    [Params(8, 64, 1024, 16384)]
    public int Length;

    private string text;

    [GlobalSetup]
    public void Setup()
    {
        stream = new MemoryStream(8192);
        binaryWriter = new BinaryWriter(stream);
        binaryReader = new BinaryReader(stream);

        buffer = new Buffer<byte>(8192);

        bufferData = new byte[8192];

        // 生成指定长度字符串
        text = new string('A', Length);

        // 预写入数据用于 Read Benchmark
        stream.Position = 0;
        binaryWriter.Write(text);
        stream.Position = 0;

        buffer.Position = 0;
        buffer.WriteUtf8(text);
        buffer.Position = 0;
    }

    // ------------------------
    // Write Benchmark
    // ------------------------

    [BenchmarkCategory("Write")]
    [Benchmark]
    public void BinaryWriter_WriteString()
    {
        stream.Position = 0;
        binaryWriter.Write(text);
    }

    [BenchmarkCategory("Write")]
    [Benchmark]
    public void Buffer_WriteUtf8()
    {
        buffer.Position = 0;
        buffer.WriteUtf8(text);
    }

    // ------------------------
    // Read Benchmark
    // ------------------------

    [BenchmarkCategory("Read")]
    [Benchmark]
    public void BinaryReader_ReadString()
    {
        stream.Position = 0;
        binaryReader.ReadString();
    }

    [BenchmarkCategory("Read")]
    [Benchmark]
    public void Buffer_ReadUtf8()
    {
        buffer.Position = 0;
        buffer.ReadUtf8();
    }
}
