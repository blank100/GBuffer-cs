using System.Runtime.CompilerServices;

namespace Gal.Core
{
	/// <summary>
	/// ZigTag算法,将有符号的整数编码为无符号整数,便于使用 Varint 编码进行压缩
	/// </summary>
	/// <para>author gouanlin</para>
	public static class ZigZagUtils
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int DecodeZigZag32(uint n) => (int)(n >> 1) ^ -(int)(n & 1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long DecodeZigZag64(ulong n) => (long)(n >> 1) ^ -(long)(n & 1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint EncodeZigZag32(int n) => (uint)((n << 1) ^ (n >> 31));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong EncodeZigZag64(long n) => (ulong)((n << 1) ^ (n >> 63));
	}
}