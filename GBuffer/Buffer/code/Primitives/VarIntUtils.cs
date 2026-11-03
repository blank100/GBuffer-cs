using System.Numerics;
using System.Runtime.InteropServices;

namespace Gal.Core;

public static partial class SpanByteUtils {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVarUInt32Size(uint value) {
#if NET8_0_OR_GREATER
        // 等价于链式阈值判断，但只需一条 lzcnt 指令：size = ceil(bits/7)，bits = 32 - LeadingZeroCount(value)
        var bits = 32 - BitOperations.LeadingZeroCount(value);
        return bits <= 7 ? 1 : (bits + 6) / 7;
#else
        if (value < 1u << 7) return 1;
        if (value < 1u << 14) return 2;
        if (value < 1u << 21) return 3;
        if (value < 1u << 28) return 4;
        return 5;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVarInt32Size(int value) => GetVarUInt32Size(ZigZagUtils.EncodeZigZag32(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVarUInt64Size(ulong value) {
#if NET8_0_OR_GREATER
        // 等价于链式阈值判断，但只需一条 lzcnt 指令：size = ceil(bits/7)，bits = 64 - LeadingZeroCount(value)
        var bits = 64 - BitOperations.LeadingZeroCount(value);
        return bits <= 7 ? 1 : (bits + 6) / 7;
#else
        if (value < 1UL << 7) return 1;
        if (value < 1UL << 14) return 2;
        if (value < 1UL << 21) return 3;
        if (value < 1UL << 28) return 4;
        if (value < 1UL << 35) return 5;
        if (value < 1UL << 42) return 6;
        if (value < 1UL << 49) return 7;
        if (value < 1UL << 56) return 8;
        if (value < 1UL << 63) return 9;
        return 10;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVarInt64Size(long value) => GetVarUInt64Size(ZigZagUtils.EncodeZigZag64(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadVarInt32(ReadOnlySpan<byte> span, out int readCount) =>
        ZigZagUtils.DecodeZigZag32(ReadVarUInt32(span, out readCount));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadVarUInt32(ReadOnlySpan<byte> span, out int readCount) {
        // 注意：因为我们要一次性读 ulong (8字节)，所以 Fast 分支的安全长度必须是 >= 8
        return BitConverter.IsLittleEndian && span.Length >= 8 ? ReadVarUInt32Fast(span, out readCount) : ReadVarUInt32Slow(span, out readCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadVarUInt32Fast(ReadOnlySpan<byte> span, out int readCount) {
        ref var b = ref MemoryMarshal.GetReference(span);

        // 只访问 1 次内存，直接拉取 8 字节进 64 位 CPU 寄存器！
        var reg = Unsafe.ReadUnaligned<ulong>(ref b);

        // 后续所有操作全在寄存器内完成，速度极快
        var b0 = (uint)reg & 0xFF;
        if ((b0 & 0x80) == 0) {
            readCount = 1;
            return b0;
        }

        var res = (uint)(b0 & 0x7F);

        var b1 = (uint)(reg >> 8) & 0xFF;
        res |= (b1 & 0x7F) << 7;
        if ((b1 & 0x80) == 0) {
            readCount = 2;
            return res;
        }

        var b2 = (uint)(reg >> 16) & 0xFF;
        res |= (b2 & 0x7F) << 14;
        if ((b2 & 0x80) == 0) {
            readCount = 3;
            return res;
        }

        var b3 = (uint)(reg >> 24) & 0xFF;
        res |= (b3 & 0x7F) << 21;
        if ((b3 & 0x80) == 0) {
            readCount = 4;
            return res;
        }

        var b4 = (uint)(reg >> 32) & 0xFF;
        if ((b4 & 0xF0) != 0) throw new FormatException("Invalid VarUInt32");
        res |= (b4 & 0x0F) << 28; // 第 5 个字节最多用 4 bit
        readCount = 5;
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadVarUInt32Slow(ReadOnlySpan<byte> span, out int readCount) {
        uint result = 0;
        var shift = 0;

        var count = Math.Min(span.Length, 5);
        ref var rb = ref MemoryMarshal.GetReference(span);
        for (var i = 0; i < count; i++) {
            var b = Unsafe.Add(ref rb, i);
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) {
                if (i == 4 && (b & 0xF0) != 0) throw new FormatException("Invalid VarUInt32");

                readCount = i + 1;
                return result;
            }

            shift += 7;
        }

        throw new FormatException("Invalid VarUInt32");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadVarInt64(ReadOnlySpan<byte> span, out int readCount) =>
        ZigZagUtils.DecodeZigZag64(ReadVarUInt64(span, out readCount));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReadVarUInt64(ReadOnlySpan<byte> span, out int readCount) {
        // 需要 >= 10 字节保证 1次 ulong + 1次 ushort 的安全读取
        return BitConverter.IsLittleEndian && span.Length >= 10 ? ReadVarUInt64Fast(span, out readCount) : ReadVarUInt64Slow(span, out readCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadVarUInt64Fast(ReadOnlySpan<byte> span, out int readCount) {
        ref var b = ref MemoryMarshal.GetReference(span);

        // 只访问 1 次内存，直接拉取 8 字节进 64 位 CPU 寄存器！
        var reg1 = Unsafe.ReadUnaligned<ulong>(ref b);

        var b0 = reg1 & 0xFF;
        if ((b0 & 0x80) == 0) {
            readCount = 1;
            return b0;
        }

        var res = b0 & 0x7F;

        var b1 = (reg1 >> 8) & 0xFF;
        res |= (b1 & 0x7F) << 7;
        if ((b1 & 0x80) == 0) {
            readCount = 2;
            return res;
        }

        var b2 = (reg1 >> 16) & 0xFF;
        res |= (b2 & 0x7F) << 14;
        if ((b2 & 0x80) == 0) {
            readCount = 3;
            return res;
        }

        var b3 = (reg1 >> 24) & 0xFF;
        res |= (b3 & 0x7F) << 21;
        if ((b3 & 0x80) == 0) {
            readCount = 4;
            return res;
        }

        var b4 = (reg1 >> 32) & 0xFF;
        res |= (b4 & 0x7F) << 28;
        if ((b4 & 0x80) == 0) {
            readCount = 5;
            return res;
        }

        var b5 = (reg1 >> 40) & 0xFF;
        res |= (b5 & 0x7F) << 35;
        if ((b5 & 0x80) == 0) {
            readCount = 6;
            return res;
        }

        var b6 = (reg1 >> 48) & 0xFF;
        res |= (b6 & 0x7F) << 42;
        if ((b6 & 0x80) == 0) {
            readCount = 7;
            return res;
        }

        var b7 = (reg1 >> 56) & 0xFF;
        res |= (b7 & 0x7F) << 49;
        if ((b7 & 0x80) == 0) {
            readCount = 8;
            return res;
        }

        // 第二次访问内存：读取最后 2 个字节
        var reg2 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref b, 8));

        var b8 = (ulong)reg2 & 0xFF;
        res |= (b8 & 0x7F) << 56;
        if ((b8 & 0x80) == 0) {
            readCount = 9;
            return res;
        }

        var b9 = (ulong)reg2 >> 8;
        if ((b9 & 0xFE) != 0) throw new FormatException("Invalid VarUInt64: Overflows 64-bit range.");
        res |= (b9 & 0x01) << 63; // 第 10 个字节最多用 1 bit
        readCount = 10;
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadVarUInt64Slow(ReadOnlySpan<byte> span, out int readCount) {
        ulong result = 0;
        var shift = 0;

        var count = Math.Min(span.Length, 10);
        ref var rb = ref MemoryMarshal.GetReference(span);
        for (var i = 0; i < count; i++) {
            var b = Unsafe.Add(ref rb, i);
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) {
                if (i == 9 && (b & 0xFE) != 0) throw new FormatException("Invalid VarUInt64: Overflows 64-bit range.");

                readCount = i + 1;
                return result;
            }

            shift += 7;
        }

        throw new FormatException("Invalid VarUInt64: Sequence too long or buffer exhausted.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteVarUInt32(Span<byte> span, uint value) {
        // Fast core: the caller must provide enough space for the actual encoded value.
        ref var b = ref MemoryMarshal.GetReference(span);

        if (value < 0x80) {
            b = (byte)value;
            return 1;
        }

        Unsafe.Add(ref b, 0) = (byte)(value | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 1) = (byte)value;
            return 2;
        }

        Unsafe.Add(ref b, 1) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 2) = (byte)value;
            return 3;
        }

        Unsafe.Add(ref b, 2) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 3) = (byte)value;
            return 4;
        }

        Unsafe.Add(ref b, 3) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        Unsafe.Add(ref b, 4) = (byte)value;
        return 5;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteVarInt32(Span<byte> span, int value) => WriteVarUInt32(span, ZigZagUtils.EncodeZigZag32(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteVarUInt64(Span<byte> span, ulong value) {
        // Fast core: the caller must provide enough space for the actual encoded value.
        ref var b = ref MemoryMarshal.GetReference(span);

        if (value < 0x80) {
            b = (byte)value;
            return 1;
        }

        Unsafe.Add(ref b, 0) = (byte)(value | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 1) = (byte)value;
            return 2;
        }

        Unsafe.Add(ref b, 1) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 2) = (byte)value;
            return 3;
        }

        Unsafe.Add(ref b, 2) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 3) = (byte)value;
            return 4;
        }

        Unsafe.Add(ref b, 3) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 4) = (byte)value;
            return 5;
        }

        Unsafe.Add(ref b, 4) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 5) = (byte)value;
            return 6;
        }

        Unsafe.Add(ref b, 5) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 6) = (byte)value;
            return 7;
        }

        Unsafe.Add(ref b, 6) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 7) = (byte)value;
            return 8;
        }

        Unsafe.Add(ref b, 7) = (byte)((value & 0x7F) | 0x80);
        value >>= 7;
        if (value < 0x80) {
            Unsafe.Add(ref b, 8) = (byte)value;
            return 9;
        }

        Unsafe.Add(ref b, 8) = (byte)((value & 0x7F) | 0x80);
        Unsafe.Add(ref b, 9) = (byte)(value >> 7);
        return 10;
    }[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteVarInt64(Span<byte> span, long value) => WriteVarUInt64(span, ZigZagUtils.EncodeZigZag64(value));
}
