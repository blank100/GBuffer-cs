using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using Gal.Core;
using SIE.IO;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[Config(typeof(BenchmarkConfig))]
public class VarInt32Benchmark {
    private ByteArray _byteArrayRead;
    private ByteArray _byteArrayWrite;

    private Buffer<byte> _bufferRead;
    private Buffer<byte> _bufferWrite;

    private readonly int[] _testValues = { 0, 1, 128, 32768, 80388608, -1, -127, -32767, -80388607 };

    private const int ValueCount = 9;
    private const int ITERATIONS = 100_000;

    [GlobalSetup]
    public void Setup() {
        if (_testValues.Length != ValueCount) throw new InvalidOperationException("ValueCount does not match _testValues.");

        var maxBytesPerInt = 5; // VarInt32 最大 5 字节
        var size = ITERATIONS * ValueCount * maxBytesPerInt;

        _byteArrayRead = new ByteArray(size);
        _byteArrayWrite = new ByteArray(size);

        _bufferRead = new Buffer<byte>(size);
        _bufferWrite = new Buffer<byte>(size);

        // 只为读取基准准备缓冲区
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var value in _testValues) {
                _byteArrayRead.WriteULEB128(value);
                _bufferRead.WriteVarInt32(value);
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

    [Benchmark(Baseline = true, OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteULEB128() {
        var values = _testValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _byteArrayWrite.WriteULEB128(v);
        }

        return _byteArrayWrite.Position;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteVarInt32() {
        var values = _testValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _bufferWrite.WriteVarInt32(v);
        }

        return _bufferWrite.Position;
    }

    // ------------------
    // Read 基准
    // ------------------

    [Benchmark(Baseline = true, OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Read")]
    public int ByteArray_ReadULEB128() {
        _byteArrayRead.Position = 0;
        var result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                result ^= _byteArrayRead.ReadULEB128();
            }
        }

        return result;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Read")]
    public int Buffer_ReadVarInt32() {
        _bufferRead.Position = 0;
        var result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                result ^= _bufferRead.ReadVarInt32();
            }
        }

        return result;
    }
}
