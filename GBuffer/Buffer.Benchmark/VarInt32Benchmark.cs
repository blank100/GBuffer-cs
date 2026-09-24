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
    private Buffer<byte> _bufferVarIntRead;
    private Buffer<byte> _bufferVarIntWrite;
    private readonly uint[] _testValues = {
        0,
        1,
        127,
        128,
        16383,
        16384,
        2097151,
        2097152,
        268435455,
        268435456,
        uint.MaxValue
    };
    private readonly int[] _signedTestValues = {
        int.MinValue,
        -80388607,
        -32767,
        -127,
        -1,
        0,
        1,
        128,
        32768,
        80388608,
        int.MaxValue
    };

    private const int ValueCount = 11;
    private const int SignedValueCount = 11;
    private const int ITERATIONS = 100_000;

    [GlobalSetup]
    public void Setup() {
        if (_testValues.Length != ValueCount) throw new InvalidOperationException("ValueCount does not match _testValues.");

        var maxBytesPerInt = 5; // VarUInt32 最大 5 字节
        var size = ITERATIONS * ValueCount * maxBytesPerInt;

        _byteArrayRead = new ByteArray(size);
        _byteArrayWrite = new ByteArray(size);

        _bufferRead = new Buffer<byte>(size);
        _bufferWrite = new Buffer<byte>(size);

        var signedSize = ITERATIONS * SignedValueCount * maxBytesPerInt;
        _bufferVarIntRead = new Buffer<byte>(signedSize);
        _bufferVarIntWrite = new Buffer<byte>(signedSize);

        // 只为读取基准准备缓冲区
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var value in _testValues) {
                _byteArrayRead.WriteULEB128(unchecked((int)value));
                _bufferRead.WriteVarUInt32(value);
            }

            foreach (var value in _signedTestValues) {
                _bufferVarIntRead.WriteVarInt32(value);
            }
        }
    }

    [IterationSetup]
    public void IterationSetup() {
        _byteArrayRead.Position = 0;
        _byteArrayWrite.Position = 0;
        _bufferRead.Position = 0;
        _bufferWrite.Position = 0;
        _bufferVarIntRead.Position = 0;
        _bufferVarIntWrite.Position = 0;
    }

    // ------------------
    // Write 基准
    // ------------------

    [Benchmark(Baseline = true, OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteULEB128() {
        var values = _testValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _byteArrayWrite.WriteULEB128(unchecked((int)v));
        }

        return _byteArrayWrite.Position;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteVarUInt32() {
        var values = _testValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _bufferWrite.WriteVarUInt32(v);
        }

        return _bufferWrite.Position;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * SignedValueCount)]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteVarInt32() {
        var values = _signedTestValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _bufferVarIntWrite.WriteVarInt32(v);
        }

        return _bufferVarIntWrite.Position;
    }

    // ------------------
    // Read 基准
    // ------------------

    [Benchmark(Baseline = true, OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Read")]
    public uint ByteArray_ReadULEB128() {
        _byteArrayRead.Position = 0;
        uint result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                result ^= unchecked((uint)_byteArrayRead.ReadULEB128());
            }
        }

        return result;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Read")]
    public uint Buffer_ReadVarUInt32() {
        _bufferRead.Position = 0;
        uint result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                result ^= _bufferRead.ReadVarUInt32();
            }
        }

        return result;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * SignedValueCount)]
    [BenchmarkCategory("Read")]
    public int Buffer_ReadVarInt32() {
        _bufferVarIntRead.Position = 0;
        var result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _signedTestValues) {
                result ^= _bufferVarIntRead.ReadVarInt32();
            }
        }

        return result;
    }

}
