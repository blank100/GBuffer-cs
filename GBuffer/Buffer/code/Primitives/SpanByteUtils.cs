using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Gal.Core {
    public static class SpanByteUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(ReadOnlySpan<byte> span, bool bigEndian = false) {
            var value = Unsafe.ReadUnaligned<ushort>(ref MemoryMarshal.GetReference(span));
            return bigEndian == BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(ReadOnlySpan<byte> span, bool bigEndian = false) => (short)ReadUInt16(span, bigEndian);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(ReadOnlySpan<byte> span, bool bigEndian = false) {
            var value = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(span));
            return bigEndian == BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(ReadOnlySpan<byte> span, bool bigEndian = false) => (int)ReadUInt32(span, bigEndian);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(ReadOnlySpan<byte> span, bool bigEndian = false) {
            var value = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(span));
            return bigEndian == BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(ReadOnlySpan<byte> span, bool bigEndian = false) => (long)ReadUInt64(span, bigEndian);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat(ReadOnlySpan<byte> span) => BitConverter.Int32BitsToSingle(ReadInt32(span));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble(ReadOnlySpan<byte> span) => BitConverter.Int64BitsToDouble(ReadInt64(span));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUInt16(Span<byte> span, ushort value, bool bigEndian = false) {
            if (bigEndian == BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            return sizeof(ushort);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteInt16(Span<byte> span, short value, bool bigEndian = false) => WriteUInt16(span, (ushort)value, bigEndian);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUInt32(Span<byte> span, uint value, bool bigEndian = false) {
            if (bigEndian == BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            return sizeof(uint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteInt32(Span<byte> span, int value, bool bigEndian = false) => WriteUInt32(span, (uint)value, bigEndian);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUInt64(Span<byte> span, ulong value, bool bigEndian = false) {
            if (bigEndian == BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
            return sizeof(ulong);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteInt64(Span<byte> span, long value, bool bigEndian = false) => WriteUInt64(span, (ulong)value, bigEndian);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteFloat(Span<byte> span, float value) => WriteInt32(span, BitConverter.SingleToInt32Bits(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteDouble(Span<byte> span, double value) => WriteInt64(span, BitConverter.DoubleToInt64Bits(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteVarUInt32(Span<byte> span, uint value) {
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteVarInt64(Span<byte> span, long value) => WriteVarUInt64(span, ZigZagUtils.EncodeZigZag64(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadVarUInt32(ReadOnlySpan<byte> span, out int readCount) {
            return span.Length >= 5
                ? ReadVarUInt32Fast(span, out readCount)
                : ReadVarUInt32Slow(span, out readCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReadVarUInt32Fast(ReadOnlySpan<byte> span, out int readCount) {
            ref var b = ref MemoryMarshal.GetReference(span);

            var byte0 = b;
            if ((byte0 & 0x80) == 0) {
                readCount = 1;
                return byte0;
            }

            var byte1 = Unsafe.Add(ref b, 1);
            var result = (uint)(byte0 & 0x7F) | ((uint)(byte1 & 0x7F) << 7);
            if ((byte1 & 0x80) == 0) {
                readCount = 2;
                return result;
            }

            var byte2 = Unsafe.Add(ref b, 2);
            result |= (uint)(byte2 & 0x7F) << 14;
            if ((byte2 & 0x80) == 0) {
                readCount = 3;
                return result;
            }

            var byte3 = Unsafe.Add(ref b, 3);
            result |= (uint)(byte3 & 0x7F) << 21;
            if ((byte3 & 0x80) == 0) {
                readCount = 4;
                return result;
            }

            var byte4 = Unsafe.Add(ref b, 4);
            if ((byte4 & 0xF0) != 0) throw new FormatException("Invalid VarUInt32");
            result |= (uint)(byte4 & 0x0F) << 28;

            readCount = 5;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadVarUInt32Slow(ReadOnlySpan<byte> span, out int readCount) {
            uint result = 0;
            var shift = 0;

            var count = Math.Min(span.Length, 5);
            for (var i = 0; i < count; i++) {
                var b = span[i];
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
        public static int ReadVarInt32(ReadOnlySpan<byte> span, out int readCount) {
            return ZigZagUtils.DecodeZigZag32(ReadVarUInt32(span, out readCount));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadVarUInt64(ReadOnlySpan<byte> span, out int readCount) {
            return span.Length >= 10
                ? ReadVarUInt64Fast(span, out readCount)
                : ReadVarUInt64Slow(span, out readCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadVarUInt64Fast(ReadOnlySpan<byte> span, out int readCount) {
            ref var b = ref MemoryMarshal.GetReference(span);

            var byte0 = b;
            if ((byte0 & 0x80) == 0) {
                readCount = 1;
                return byte0;
            }

            var byte1 = Unsafe.Add(ref b, 1);
            var result = ((ulong)byte0 & 0x7FUL) | ((ulong)(byte1 & 0x7F) << 7);
            if ((byte1 & 0x80) == 0) {
                readCount = 2;
                return result;
            }

            var byte2 = Unsafe.Add(ref b, 2);
            result |= (ulong)(byte2 & 0x7F) << 14;
            if ((byte2 & 0x80) == 0) {
                readCount = 3;
                return result;
            }

            var byte3 = Unsafe.Add(ref b, 3);
            result |= (ulong)(byte3 & 0x7F) << 21;
            if ((byte3 & 0x80) == 0) {
                readCount = 4;
                return result;
            }

            var byte4 = Unsafe.Add(ref b, 4);
            result |= (ulong)(byte4 & 0x7F) << 28;
            if ((byte4 & 0x80) == 0) {
                readCount = 5;
                return result;
            }

            var byte5 = Unsafe.Add(ref b, 5);
            result |= (ulong)(byte5 & 0x7F) << 35;
            if ((byte5 & 0x80) == 0) {
                readCount = 6;
                return result;
            }

            var byte6 = Unsafe.Add(ref b, 6);
            result |= (ulong)(byte6 & 0x7F) << 42;
            if ((byte6 & 0x80) == 0) {
                readCount = 7;
                return result;
            }

            var byte7 = Unsafe.Add(ref b, 7);
            result |= (ulong)(byte7 & 0x7F) << 49;
            if ((byte7 & 0x80) == 0) {
                readCount = 8;
                return result;
            }

            var byte8 = Unsafe.Add(ref b, 8);
            result |= (ulong)(byte8 & 0x7F) << 56;
            if ((byte8 & 0x80) == 0) {
                readCount = 9;
                return result;
            }

            var byte9 = Unsafe.Add(ref b, 9);
            if ((byte9 & 0xFE) != 0) throw new FormatException("Invalid VarUInt64: Overflows 64-bit range.");
            result |= (ulong)(byte9 & 0x01) << 63;

            readCount = 10;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadVarUInt64Slow(ReadOnlySpan<byte> span, out int readCount) {
            ulong result = 0;
            var shift = 0;

            var count = Math.Min(span.Length, 10);
            for (var i = 0; i < count; i++) {
                var b = span[i];
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
        public static long ReadVarInt64(ReadOnlySpan<byte> span, out int readCount) {
            return ZigZagUtils.DecodeZigZag64(ReadVarUInt64(span, out readCount));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteSleb128Int32(Span<byte> span, int value) {
            var count = 0;
            while (true) {
                var b = (byte)(value & 0x7F);
                value >>= 7;

                var done = (value == 0 && (b & 0x40) == 0)
                           || (value == -1 && (b & 0x40) != 0);
                if (!done) b |= 0x80;

                span[count++] = b;
                if (done) return count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteSleb128Int64(Span<byte> span, long value) {
            var count = 0;
            while (true) {
                var b = (byte)(value & 0x7F);
                value >>= 7;

                var done = (value == 0 && (b & 0x40) == 0)
                           || (value == -1 && (b & 0x40) != 0);
                if (!done) b |= 0x80;

                span[count++] = b;
                if (done) return count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadSleb128Int32(ReadOnlySpan<byte> span, out int readCount) {
            long result = 0;
            var shift = 0;
            var count = 0;

            for (var i = 0; i < 5; i++) {
                if ((uint)count >= (uint)span.Length) throw new FormatException("Invalid SLEB128 Int32.");

                var b = span[count++];
                result |= (long)(b & 0x7F) << shift;
                shift += 7;

                if ((b & 0x80) == 0) {
                    if ((b & 0x40) != 0) result |= -1L << shift;
                    if (result < int.MinValue || result > int.MaxValue) throw new FormatException("Invalid SLEB128 Int32.");

                    readCount = count;
                    return (int)result;
                }
            }

            throw new FormatException("Invalid SLEB128 Int32.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadSleb128Int64(ReadOnlySpan<byte> span, out int readCount) {
            ulong result = 0;
            var shift = 0;
            var count = 0;

            for (var i = 0; i < 10; i++) {
                if ((uint)count >= (uint)span.Length) throw new FormatException("Invalid SLEB128 Int64.");

                var b = span[count++];
                if (i == 9) {
                    if ((b & 0x80) != 0) throw new FormatException("Invalid SLEB128 Int64.");

                    var signBits = b & 0x7E;
                    if (signBits != 0 && signBits != 0x7E) throw new FormatException("Invalid SLEB128 Int64.");
                }

                result |= (ulong)(b & 0x7F) << shift;
                shift += 7;

                if ((b & 0x80) == 0) {
                    if (shift < 64 && (b & 0x40) != 0) result |= ulong.MaxValue << shift;

                    readCount = count;
                    return unchecked((long)result);
                }
            }

            throw new FormatException("Invalid SLEB128 Int64.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadUtf8(ReadOnlySpan<byte> span, int length) {
            if (length == 0) return string.Empty;
            if ((uint)length > (uint)span.Length) throw new FormatException("Invalid UTF-8 length.");
            return Encoding.UTF8.GetString(span.Slice(0, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadUtf8(ReadOnlySpan<byte> span, out int readCount) {
            if (span.Length < sizeof(ushort)) throw new FormatException("Invalid UTF-8 length.");
            var length = ReadUInt16(span);
            readCount = sizeof(ushort) + length;
            if (readCount > span.Length) throw new FormatException("Invalid UTF-8 length.");
            return Encoding.UTF8.GetString(span.Slice(sizeof(ushort), length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUtf8(Span<byte> span, string value) {
            if (string.IsNullOrEmpty(value)) {
                WriteUInt16(span, 0);
                return sizeof(ushort);
            }

            var count = Encoding.UTF8.GetBytes(value.AsSpan(), span.Slice(sizeof(ushort)));
            if (count > short.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
            WriteUInt16(span, (ushort)count);
            return sizeof(ushort) + count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAscii(ReadOnlySpan<byte> span) {
            foreach (var b in span) {
                if (b > 127) return false;
            }

            return true;
        }
    }
}
