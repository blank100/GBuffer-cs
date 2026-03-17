namespace Gal.Core {
    /// <summary>
    ///
    /// </summary>
    /// <author>gouanlin</author>
    public static class BytesWriter {
        static BytesWriter() {
            if(!BitConverter.IsLittleEndian) throw new NotSupportedException("Big endian is not supported");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteUInt8(ref byte* bytes, byte value) => *bytes++ = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteInt8(ref byte* bytes, sbyte value) => *bytes++ = (byte)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteBoolean(ref byte* bytes, bool value) => *bytes++ = value ? (byte)1 : (byte)0;

        // 泛型写入优化：利用 Unsafe.SizeOf 避免运行时计算，并显式指定内存写入
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(ref byte* bytes, T value) where T : unmanaged {
            *(T*)bytes = value;
            bytes += Unsafe.SizeOf<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteInt16(ref byte* bytes, short value) {
            var t = value;
#if BIGENDIAN
            t = BinaryPrimitives.ReverseEndianness(t);
#endif
            *(short*)bytes = t;
            bytes += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteUInt16(ref byte* bytes, ushort value) {
            var t = value;
#if BIGENDIAN
            t = BinaryPrimitives.ReverseEndianness(t);
#endif
            *(ushort*)bytes = t;
            bytes += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteInt32(ref byte* bytes, int value) {
#if BIGENDIAN
			*(int*) bytes = BinaryPrimitives.ReverseEndianness(value);
#else
            *(int*)bytes = value;
#endif
            bytes += 4;
        }

        public static unsafe void WriteUInt32(ref byte* bytes, uint value) {
#if BIGENDIAN
			*(uint*) bytes = BinaryPrimitives.ReverseEndianness(value);
#else
            *(uint*)bytes = value;
#endif
            bytes += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteInt64(ref byte* bytes, long value) {
#if BIGENDIAN
			*(long*) bytes = BinaryPrimitives.ReverseEndianness(value);
#else
            *(long*)bytes = value;
#endif
            bytes += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteUInt64(ref byte* bytes, ulong value) {
#if BIGENDIAN
			*(ulong*) bytes = BinaryPrimitives.ReverseEndianness(value);
#else
            *(ulong*)bytes = value;
#endif
            bytes += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteFloat(ref byte* bytes, float value)
            => WriteInt32(ref bytes, BitConverter.SingleToInt32Bits(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteDouble(ref byte* bytes, double value)
            => WriteInt64(ref bytes, BitConverter.DoubleToInt64Bits(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteVarUInt32(ref byte* bytes, uint value) {
			//@formatter:off
            if (value < 0x80) { *bytes++ = (byte)value; return; }

            *bytes++ = (byte)(value | 0x80); value >>= 7;
            if (value < 0x80) { *bytes++ = (byte)value; return; }

            *bytes++ = (byte)(value | 0x80); value >>= 7;
            if (value < 0x80) { *bytes++ = (byte)value; return; }

            *bytes++ = (byte)(value | 0x80); value >>= 7;
            if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80); value >>= 7;
            *bytes++ = (byte)(value);
            //@formatter:on
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteVarInt32(ref byte* bytes, int value) => WriteVarUInt32(ref bytes, ZigZagUtils.EncodeZigZag32(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteVarUInt64(ref byte* bytes, ulong value) {
			//@formatter:off
			if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80); value >>= 7;
			if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80); value >>= 7;
			if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80); value >>= 7;
			if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80); value >>= 7;
			if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80); value >>= 7;
			if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80); value >>= 7;
			if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80); value >>= 7;
			if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80); value >>= 7;
			if (value < 0x80) { *bytes++ = (byte)value; return; }

			*bytes++ = (byte)(value | 0x80);
			*bytes++ = (byte)(value >> 7);
            //@formatter:on
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteVarInt64(ref byte* bytes, long value) => WriteVarUInt64(ref bytes, ZigZagUtils.EncodeZigZag64(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteUtf8WithoutLength(ref byte* bytes, string value) {
            var bytesCount = System.Text.Encoding.UTF8.GetByteCount(value);
            fixed (char* source = value) System.Text.Encoding.UTF8.GetBytes(source, value.Length, bytes, bytesCount);
            bytes += bytesCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteUtf8(ref byte* bytes, string value) {
            if (string.IsNullOrEmpty(value)) {
                WriteUInt16(ref bytes, 0);
                return;
            }

            ushort bytesCount = (ushort)System.Text.Encoding.UTF8.GetByteCount(value);
            WriteUInt16(ref bytes, bytesCount);
            fixed (char* source = value) System.Text.Encoding.UTF8.GetBytes(source, value.Length, bytes, bytesCount);
            bytes += bytesCount;
        }
    }
}
