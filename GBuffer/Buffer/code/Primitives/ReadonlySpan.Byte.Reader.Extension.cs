using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gal.Core
{
	/// <summary>
	/// 
	/// </summary>
	/// <para>author gouanlin</para>
	public static class ReadonlySpanByteReaderExtension
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte ReadUInt8(this ref ReadOnlySpan<byte> self) {
			var t = self[0];
			self = self[1..];
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sbyte ReadInt8(this ref ReadOnlySpan<byte> self) {
			var t = (sbyte)self[0];
			self = self[1..];
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort ReadUInt16(this ref ReadOnlySpan<byte> self) {
#if BIGENDIAN
			var t = (ushort) ((self[1] << 8) | self[0]);
#else
			var t = (ushort)(self[0] | (self[1] << 8));
#endif
			self = self[2..];
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short ReadInt16(this ref ReadOnlySpan<byte> self) {
#if BIGENDIAN
			var t = (short) ((self[1] << 8) | self[0]);
#else
			var t = (short)(self[0] | (self[1] << 8));
#endif
			self = self[2..];
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint ReadUInt32(this ref ReadOnlySpan<byte> self) {
#if BIGENDIAN
			var t = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(self)));
#else
			var t = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(self));
#endif
			self = self[4..];
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ReadInt32(this ref ReadOnlySpan<byte> self) {
#if BIGENDIAN
			var t = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(self)));
#else
			var t = Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(self));
#endif
			self = self[4..];
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong ReadUInt64(this ref ReadOnlySpan<byte> self) {
#if BIGENDIAN
			var t = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(self)));
#else
			var t = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(self));
#endif
			self = self[8..];
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long ReadInt64(this ref ReadOnlySpan<byte> self) {
#if BIGENDIAN
			var t = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<long>(ref MemoryMarshal.GetReference(self)));
#else
			var t = Unsafe.ReadUnaligned<long>(ref MemoryMarshal.GetReference(self));
#endif
			self = self[8..];
			return t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe float ReadFloat(this ref ReadOnlySpan<byte> self) {
			var t = self.ReadInt32();
			return *(float*)&t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe double ReadDouble(this ref ReadOnlySpan<byte> self) {
			var t = self.ReadInt64();
			return *(double*)&t;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint ReadVarUInt32(this ref ReadOnlySpan<byte> self) {
			unsafe {
				fixed (byte* bytes = self) {
					var t = bytes;
					var result = BytesReader.ReadVarUInt32(ref t);
					self = self[(int)(t - bytes)..];
					return result;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ReadVarInt32(this ref ReadOnlySpan<byte> self) => ZigZagUtils.DecodeZigZag32(self.ReadVarUInt32());

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong ReadVarUInt64(this ref ReadOnlySpan<byte> self) {
			unsafe {
				fixed (byte* bytes = self) {
					var t = bytes;
					var result = BytesReader.ReadVarUInt64(ref t);
					self = self[(int)(t - bytes)..];
					return result;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long ReadVarInt64(this ref ReadOnlySpan<byte> self) => ZigZagUtils.DecodeZigZag64(self.ReadVarUInt64());

		public static unsafe string ReadUtf8(this ref ReadOnlySpan<byte> self, int len) {
			Debug.Assert(len <= self.Length, "read data overflow");
			fixed (byte* bytes = self) {
				var t = bytes;
				var result = BytesReader.ReadUtf8(ref t, len);
				self = self[(int)(t - bytes)..];
				return result;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ReadUtf8(this ref ReadOnlySpan<byte> self) => self.ReadUtf8(self.ReadInt16());
	}
}