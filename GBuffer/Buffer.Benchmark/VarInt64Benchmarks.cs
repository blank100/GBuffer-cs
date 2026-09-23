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
    private Buffer<byte> _bufferVarIntRead;
    private Buffer<byte> _bufferVarIntWrite;
    private Buffer<byte> _bufferSleb128Read;
    private Buffer<byte> _bufferSleb128Write;
    private readonly ulong[] _testValues = {
        0,
        1,
        128,
        32768,
        80388608,
        2147483648UL,
        549755813888UL,
        140737488355328UL,
        36028797018963968UL,
        9223372036854775807UL,
        9223372036854775808UL,
        ulong.MaxValue
    };
    private readonly long[] _signedTestValues = {
        long.MinValue,
        -36028797018963967L,
        -140737488355327L,
        -549755813887L,
        -80388607,
        -32767,
        -127,
        -1,
        0,
        1,
        128,
        32768,
        80388608,
        2147483648L,
        549755813888L,
        140737488355328L,
        36028797018963968L,
        long.MaxValue
    };

    private const int ValueCount = 12;
    private const int SignedValueCount = 18;
    private const int ITERATIONS = 20_000;

    [GlobalSetup]
    public void Setup() {
        if (_testValues.Length != ValueCount) throw new InvalidOperationException("ValueCount does not match _testValues.");

        int maxBytesPerInt = 10; // VarUInt64 最大 10 字节
        int size = ITERATIONS * ValueCount * maxBytesPerInt;

        _byteArrayRead = new ByteArray(size);
        _byteArrayWrite = new ByteArray(size);

        _bufferRead = new Buffer<byte>(size);
        _bufferWrite = new Buffer<byte>(size);

        var signedSize = ITERATIONS * SignedValueCount * maxBytesPerInt;
        _bufferVarIntRead = new Buffer<byte>(signedSize);
        _bufferVarIntWrite = new Buffer<byte>(signedSize);
        _bufferSleb128Read = new Buffer<byte>(signedSize);
        _bufferSleb128Write = new Buffer<byte>(signedSize);

        // 只为读取基准准备缓冲区
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var value in _testValues) {
                _byteArrayRead.WriteULEB128(unchecked((long)value));
                _bufferRead.WriteVarUInt64(value);
            }

            foreach (var value in _signedTestValues) {
                _bufferVarIntRead.WriteVarInt64(value);
                _bufferSleb128Read.WriteSleb128Int64(value);
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
        _bufferSleb128Read.Position = 0;
        _bufferSleb128Write.Position = 0;
    }

    // ------------------
    // Write 基准
    // ------------------

    [Benchmark(Baseline = true, OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Write")]
    public long ByteArray_WriteULEB128() {
        var values = _testValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _byteArrayWrite.WriteULEB128(unchecked((long)v));
        }

        return _byteArrayWrite.Position;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteVarUInt64() {
        var values = _testValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _bufferWrite.WriteVarUInt64(v);
        }

        return _bufferWrite.Position;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * SignedValueCount)]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteVarInt64() {
        var values = _signedTestValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _bufferVarIntWrite.WriteVarInt64(v);
        }

        return _bufferVarIntWrite.Position;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * SignedValueCount)]
    [BenchmarkCategory("Write")]
    public long Buffer_WriteSleb128Int64() {
        var values = _signedTestValues;
        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in values) _bufferSleb128Write.WriteSleb128Int64(v);
        }

        return _bufferSleb128Write.Position;
    }

    // ------------------
    // Read 基准
    // ------------------

    [Benchmark(Baseline = true, OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Read")]
    public ulong ByteArray_ReadULEB128() {
        _byteArrayRead.Position = 0;
        ulong result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                result ^= unchecked((ulong)_byteArrayRead.ReadULEB128_Int64());
            }
        }

        return result;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * ValueCount)]
    [BenchmarkCategory("Read")]
    public ulong Buffer_ReadVarUInt64() {
        _bufferRead.Position = 0;
        ulong result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _testValues) {
                result ^= _bufferRead.ReadVarUInt64();
            }
        }

        return result;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * SignedValueCount)]
    [BenchmarkCategory("Read")]
    public long Buffer_ReadVarInt64() {
        _bufferVarIntRead.Position = 0;
        long result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _signedTestValues) {
                result ^= _bufferVarIntRead.ReadVarInt64();
            }
        }

        return result;
    }

    [Benchmark(OperationsPerInvoke = ITERATIONS * SignedValueCount)]
    [BenchmarkCategory("Read")]
    public long Buffer_ReadSleb128Int64() {
        _bufferSleb128Read.Position = 0;
        long result = 0;

        for (var i = 0; i < ITERATIONS; i++) {
            foreach (var v in _signedTestValues) {
                result ^= _bufferSleb128Read.ReadSleb128Int64();
            }
        }

        return result;
    }
}
