using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using Gal.Core;
using SIE.IO;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class VarInt64Benchmark {
    private ByteArray _byteArrayRead;
    private ByteArray _byteArrayWrite;

    private Buffer<byte> _bufferRead;
    private Buffer<byte> _bufferWrite;

    private readonly long[] _testValues = {
        0,
        1,
        128,
        32768,
        80388608,
        2147483648L,
        549755813888L,
        140737488355328L,
        36028797018963968L,
        9223372036854775807L
        -1,
        -127,
        -32767,
        -80388607,
        -549755813887L,
        -140737488355327L,
        -36028797018963967L,
        -9223372036854775807L
    };

    private const int ITERATIONS = 100_000_00;

    [GlobalSetup]
    public void Setup() {
        int maxBytesPerInt = 10; // VarInt32 最大 5 字节
        int size = ITERATIONS * _testValues.Length * maxBytesPerInt;

        _byteArrayRead = new ByteArray(size);
        _byteArrayWrite = new ByteArray(size);

        _bufferRead = new Buffer<byte>(size);
        _bufferWrite = new Buffer<byte>(size);

        // 只为读取基准准备缓冲区
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var value in _testValues) {
                _byteArrayRead.WriteULEB128(value);
                _bufferRead.WriteVarInt64(value);
            }
        }
    }

    [IterationSetup]
    public void IterationSetup() {
        _byteArrayRead.Position = 0;
        _byteArrayWrite.Position = 0;
        _bufferRead.Position = 0;
        _bufferWrite.Position = 0;
    }

    // ------------------
    // Write 基准
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteULEB128() {
        var values = _testValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _byteArrayWrite.WriteULEB128(v);
        }

        return _byteArrayWrite.Position;
    }

    [Benchmark]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteVarInt64() {
        var values = _testValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _bufferWrite.WriteVarInt64(v);
        }

        return _bufferWrite.Position;
    }

    // ------------------
    // Read 基准
    // ------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Read")]
    public long ByteArray_ReadULEB128() {
        _byteArrayRead.Position = 0;
        long result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                result ^= _byteArrayRead.ReadULEB128_Int64();
            }
        }

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Read")]
    public long Buffer_ReadVarInt64() {
        _bufferRead.Position = 0;
        long result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                result ^= _bufferRead.ReadVarInt64();
            }
        }

        return result;
    }
}
