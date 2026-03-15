using System.Runtime.InteropServices;

namespace Gal.Core {
    public static class SpanByteUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WriteVarUInt32(ref Span<byte> self, uint value) {
            //@formatter:off
            ref var b = ref MemoryMarshal.GetReference(self);

            if (value < 0x80) { b = (byte)value;  return 1; }

            Unsafe.Add(ref b, 0) = (byte)(value | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 1) = (byte)value;  return 2; }

            Unsafe.Add(ref b, 1) = (byte)((value & 0x7F) | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 2) = (byte)value;  return 3; }

            Unsafe.Add(ref b, 2) = (byte)((value & 0x7F) | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 3) = (byte)value;  return 4; }

            Unsafe.Add(ref b, 3) = (byte)((value & 0x7F) | 0x80);value >>= 7;
            Unsafe.Add(ref b, 4) = (byte)value;

            return 5;
            //@formatter:on
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadVarUInt32(ReadOnlySpan<byte> span, out int readCount) {
			//@formatter:off
			ref var b = ref MemoryMarshal.GetReference(span);

			var byte0 = b;
			if ((byte0 & 0x80) == 0) { readCount = 1; return byte0; }

			var byte1 = Unsafe.Add(ref b, 1);
			var result = (uint)(byte0 & 0x7F) | ((uint)(byte1 & 0x7F) << 7);
			if ((byte1 & 0x80) == 0) { readCount = 2; return result; }

			var byte2 = Unsafe.Add(ref b, 2);
			result |= (uint)(byte2 & 0x7F) << 14;
			if ((byte2 & 0x80) == 0) { readCount = 3; return result; }

			var byte3 = Unsafe.Add(ref b, 3);
			result |= (uint)(byte3 & 0x7F) << 21;
			if ((byte3 & 0x80) == 0) { readCount = 4; return result; }

			var byte4 = Unsafe.Add(ref b, 4);
			result |= (uint)(byte4 & 0x0F) << 28;

			readCount = 5; return result;
            //@formatter:on
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
        public static int WriteVarUInt64(ref Span<byte> self, ulong value) {
            //@formatter:off
            ref var b = ref MemoryMarshal.GetReference(self);

            if (value < 0x80) { b = (byte)value; return 1; }

            Unsafe.Add(ref b, 0) = (byte)(value | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 1) = (byte)value; return 2; }

            Unsafe.Add(ref b, 1) = (byte)((value & 0x7F) | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 2) = (byte)value; return 3; }

            Unsafe.Add(ref b, 2) = (byte)((value & 0x7F) | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 3) = (byte)value; return 4; }

            Unsafe.Add(ref b, 3) = (byte)((value & 0x7F) | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 4) = (byte)value; return 5; }

            Unsafe.Add(ref b, 4) = (byte)((value & 0x7F) | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 5) = (byte)value; return 6; }

            Unsafe.Add(ref b, 5) = (byte)((value & 0x7F) | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 6) = (byte)value; return 7; }

            Unsafe.Add(ref b, 6) = (byte)((value & 0x7F) | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 7) = (byte)value; return 8; }

            Unsafe.Add(ref b, 7) = (byte)((value & 0x7F) | 0x80); value >>= 7;
            if (value < 0x80) { Unsafe.Add(ref b, 8) = (byte)value; return 9; }

            Unsafe.Add(ref b, 8) = (byte)((value & 0x7F) | 0x80);
            Unsafe.Add(ref b, 9) = (byte)(value >> 7);

            return 10;
            //@formatter:on
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadVarUInt64(ReadOnlySpan<byte> span, out int readCount) {
			//@formatter:off
			ref var b = ref MemoryMarshal.GetReference(span);

			var byte0 = b;
			if ((byte0 & 0x80) == 0) { readCount = 1; return byte0; }

			var byte1 = Unsafe.Add(ref b, 1);
			var result = (uint)(byte0 & 0x7F) | ((ulong)(byte1 & 0x7F) << 7);
			if ((byte1 & 0x80) == 0) { readCount = 2; return result; }

			var byte2 = Unsafe.Add(ref b, 2);
			result |= (ulong)(byte2 & 0x7F) << 14;
			if ((byte2 & 0x80) == 0) { readCount = 3; return result; }

			var byte3 = Unsafe.Add(ref b, 3);
			result |= (ulong)(byte3 & 0x7F) << 21;
			if ((byte3 & 0x80) == 0) { readCount = 4; return result; }

			var byte4 = Unsafe.Add(ref b, 4);
			result |= (ulong)(byte4 & 0x7F) << 28;
			if ((byte4 & 0x80) == 0) { readCount = 5; return result; }

			var byte5 = Unsafe.Add(ref b, 5);
			result |= (ulong)(byte5 & 0x7F) << 35;
			if ((byte5 & 0x80) == 0) { readCount = 6; return result; }

			var byte6 = Unsafe.Add(ref b, 6);
			result |= (ulong)(byte6 & 0x7F) << 42;
			if ((byte6 & 0x80) == 0) { readCount = 7; return result; }

			var byte7 = Unsafe.Add(ref b, 7);
			result |= (ulong)(byte7 & 0x7F) << 49;
			if ((byte7 & 0x80) == 0) { readCount = 8; return result; }

			var byte8 = Unsafe.Add(ref b, 8);
			result |= (ulong)(byte8 & 0x7F) << 56;
			if ((byte8 & 0x80) == 0) { readCount = 9; return result; }

			var byte9 = Unsafe.Add(ref b, 9);
			result |= (ulong)byte9 << 63;

			readCount = 10; return result;
            //@formatter:on
        }
    }
}
