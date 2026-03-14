using System.Runtime.CompilerServices;

namespace Gal.Core
{
	/// <summary>
	/// 
	/// </summary>
	/// <author>gouanlin</author>
	public static class BytesReader
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe sbyte ReadInt8(ref byte* bytes) => (sbyte)*bytes++;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe byte ReadUInt8(ref byte* bytes) => *bytes++;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe bool ReadBoolean(ref byte* bytes) => *bytes++ != 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe short ReadInt16(ref byte* bytes) {
#if BIGENDIAN
			return (short) ((*bytes++ << 8) | *bytes++);
#else
			return (short)(*bytes++ | (*bytes++ << 8));
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe ushort ReadUInt16(ref byte* bytes) {
#if BIGENDIAN
			return (ushort) ((*bytes++ << 8) | *bytes++);
#else
			return (ushort)(*bytes++ | (*bytes++ << 8));
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe int ReadInt32(ref byte* bytes) {
			var t = *(int*)bytes;
#if BIGENDIAN
			t = BinaryPrimitives.ReverseEndianness(t);
#endif
			bytes += 4;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe uint ReadUInt32(ref byte* bytes) {
			var t = *(uint*)bytes;
#if BIGENDIAN
			t = BinaryPrimitives.ReverseEndianness(t);
#endif
			bytes += 4;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe long ReadInt64(ref byte* bytes) {
			var t = *(long*)bytes;
#if BIGENDIAN
			t = BinaryPrimitives.ReverseEndianness(t);
#endif
			bytes += 8;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe ulong ReadUInt64(ref byte* bytes) {
			var t = *(ulong*)bytes;
#if BIGENDIAN
			t = BinaryPrimitives.ReverseEndianness(t);
#endif
			bytes += 8;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe float ReadFloat(ref byte* bytes) {
			var t = ReadInt32(ref bytes);
			return *(float*)&t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe double ReadDouble(ref byte* bytes) {
			var t = ReadInt64(ref bytes);
			return *(double*)&t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe uint ReadVarUInt32(ref byte* bytes) {
			var b0 = *bytes++;
			if ((b0 & 0x80) == 0) return b0;

			var b1 = *bytes++;
			var result = (uint)(b0 & 0x7F) | ((uint)(b1 & 0x7F) << 7);
			if ((b1 & 0x80) == 0) return result;

			var b2 = *bytes++;
			result |= (uint)(b2 & 0x7F) << 14;
			if ((b2 & 0x80) == 0) return result;

			var b3 = *bytes++;
			result |= (uint)(b3 & 0x7F) << 21;
			if ((b3 & 0x80) == 0) return result;

			var b4 = *bytes++;
			result |= (uint)b4 << 28;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe int ReadVarInt32(ref byte* bytes) => ZigZagUtils.DecodeZigZag32(ReadVarUInt32(ref bytes));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe ulong ReadVarUInt64(ref byte* bytes) {
			var b0 = *bytes++;
			if ((b0 & 0x80) == 0) return b0;

			var b1 = *bytes++;
			var result = (uint)(b0 & 0x7F) | ((ulong)(b1 & 0x7F) << 7);
			if ((b1 & 0x80) == 0) return result;

			var b2 = *bytes++;
			result |= (ulong)(b2 & 0x7F) << 14;
			if ((b2 & 0x80) == 0) return result;

			var b3 = *bytes++;
			result |= (ulong)(b3 & 0x7F) << 21;
			if ((b3 & 0x80) == 0) return result;

			var b4 = *bytes++;
			result |= (ulong)(b4 & 0x7F) << 28;
			if ((b4 & 0x80) == 0) return result;

			var b5 = *bytes++;
			result |= (ulong)(b5 & 0x7F) << 35;
			if ((b5 & 0x80) == 0) return result;

			var b6 = *bytes++;
			result |= (ulong)(b6 & 0x7F) << 42;
			if ((b6 & 0x80) == 0) return result;

			var b7 = *bytes++;
			result |= (ulong)(b7 & 0x7F) << 49;
			if ((b7 & 0x80) == 0) return result;

			var b8 = *bytes++;
			result |= (ulong)(b8 & 0x7F) << 56;
			if ((b8 & 0x80) == 0) return result;

			var b9 = *bytes++;
			result |= (ulong)b9 << 63;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe long ReadVarInt64(ref byte* bytes) => ZigZagUtils.DecodeZigZag64(ReadVarUInt64(ref bytes));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe string ReadUtf8(ref byte* bytes, int len) {
			if (len == 0) return string.Empty;
			var t = System.Text.Encoding.UTF8.GetString(bytes, len);
			bytes += len;
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe string ReadUtf8(ref byte* bytes) => ReadUtf8(ref bytes, ReadUInt16(ref bytes));
	}
}
