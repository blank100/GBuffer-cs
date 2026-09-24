using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Gal.Core {
    public static partial class SpanByteUtils {
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
        public static float ReadFloat(ReadOnlySpan<byte> span, bool bigEndian = false) => BitConverter.Int32BitsToSingle(ReadInt32(span, bigEndian));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble(ReadOnlySpan<byte> span, bool bigEndian = false) => BitConverter.Int64BitsToDouble(ReadInt64(span, bigEndian));

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
        public static int WriteFloat(Span<byte> span, float value, bool bigEndian = false) => WriteInt32(span, BitConverter.SingleToInt32Bits(value), bigEndian);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteDouble(Span<byte> span, double value, bool bigEndian = false) => WriteInt64(span, BitConverter.DoubleToInt64Bits(value), bigEndian);



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadUtf8(ReadOnlySpan<byte> span, int length) {
            if (length == 0) return string.Empty;
            if ((uint)length > (uint)span.Length) throw new FormatException("Invalid UTF-8 length.");
            return Encoding.UTF8.GetString(span.Slice(0, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadUtf8(ReadOnlySpan<byte> span, out int readCount, bool bigEndian = false) {
            if (span.Length < sizeof(ushort)) throw new FormatException("Invalid UTF-8 length.");
            var length = ReadUInt16(span, bigEndian);
            readCount = sizeof(ushort) + length;
            if (readCount > span.Length) throw new FormatException("Invalid UTF-8 length.");
            return Encoding.UTF8.GetString(span.Slice(sizeof(ushort), length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteUtf8(Span<byte> span, string value, bool bigEndian = false) {
            if (string.IsNullOrEmpty(value)) {
                WriteUInt16(span, 0, bigEndian);
                return sizeof(ushort);
            }

            var count = Encoding.UTF8.GetBytes(value.AsSpan(), span.Slice(sizeof(ushort)));
            if (count > short.MaxValue) throw new ArgumentOutOfRangeException(nameof(value));
            WriteUInt16(span, (ushort)count, bigEndian);
            return sizeof(ushort) + count;
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static bool IsAscii(ReadOnlySpan<byte> span) {
        //     foreach (var b in span) {
        //         if (b > 127) return false;
        //     }
        //
        //     return true;
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAscii(ReadOnlySpan<byte> span) {
            ref var b = ref MemoryMarshal.GetReference(span);
            var length = span.Length;
            var i = 0;

            // 每次处理 8 个字节（利用 64 位寄存器），提升 8 倍速度
            while (i <= length - 8) {
                var val = Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref b, i));
                // Ascii 的特征是最高位（符号位）均为 0。0x8080808080808080 用来检测 8 个字节中是否有非 Ascii
                if ((val & 0x8080808080808080ul) != 0) return false;
                i += 8;
            }

            // 处理尾部剩余的字节
            for (; i < length; i++) {
                if (Unsafe.Add(ref b, i) > 127) return false;
            }

            return true;
        }
    }
}
