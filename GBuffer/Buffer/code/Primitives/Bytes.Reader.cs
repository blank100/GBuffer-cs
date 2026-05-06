//测试用预编译常量定义
#define ENDIAN_CHECK
#define BIGENDIAN

namespace Gal.Core {
    /// <summary>
    ///
    /// </summary>
    /// <author>gouanlin</author>
    public static unsafe class BytesReader {
        static BytesReader() {
            if (!BitConverter.IsLittleEndian) throw new NotSupportedException("Big endian is not supported");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(ref byte* ptr) where T : unmanaged {
            var v = Unsafe.ReadUnaligned<T>(ptr);
            ptr += sizeof(T);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat(ref byte* ptr) => BitConverter.Int32BitsToSingle(Read<int>(ref ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble(ref byte* ptr) => BitConverter.Int64BitsToDouble(Read<long>(ref ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadVarUInt32(ref byte* ptr) {
            uint b = *ptr++;
            var res = b & 0x7F;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 7;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 14;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 21;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x0F) << 28;
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadVarInt32(ref byte* ptr) => ZigZagUtils.DecodeZigZag32(ReadVarUInt32(ref ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadVarUInt64(ref byte* ptr) {
            ulong b = *ptr++;
            ulong res = b & 0x7F;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 7;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 14;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 21;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 28;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 35;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 42;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 49;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x7F) << 56;
            if ((b & 0x80) == 0) return res;

            b = *ptr++; res |= (b & 0x01) << 63; // 第10字节仅贡献最后1位
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadVarInt64(ref byte* ptr) => ZigZagUtils.DecodeZigZag64(ReadVarUInt64(ref ptr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadUtf8(ref byte* ptr, int len) {
            if (len <= 0) return string.Empty;
            var t = System.Text.Encoding.UTF8.GetString(ptr, len);
            ptr += len;
            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadUtf8(ref byte* ptr) => ReadUtf8(ref ptr, Read<ushort>(ref ptr));
    }
}
