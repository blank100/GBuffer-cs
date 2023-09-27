using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gal.Core
{
    /// <summary>
    /// Reader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <para>author gouanlin</para>
    public class Reader<T> : IReader<T>
	{
		private readonly T[] m_Buffer;
		private int m_Position;

		public int length {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Buffer.Length;
		}

		public int position {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Position;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				Debug.Assert(0 <= value && value <= m_Buffer.Length, $"{nameof(position)} cannot be less than 0 or greater than {nameof(length)}");
				m_Position = value;
			}
		}

		public int readableCount {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Buffer.Length - m_Position;
		}

		public ReadOnlyMemory<T> memory {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Buffer.AsMemory(m_Position);
		}

		public ReadOnlySpan<T> span {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Buffer[m_Position..];
		}

		public T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Buffer[index];
		}

		public Reader(T[] original) {
			m_Buffer = original;
			m_Position = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Read() => m_Buffer[m_Position++];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Advance(int count) {
			Debug.Assert(m_Position + count >= 0, "移动后的指针位置不能未负数");
			Debug.Assert(m_Position + count <= m_Buffer.Length, "移动后的指针位置超出了buffer的容量");
			m_Position += count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlySpan<T> GetSpan(int count) {
			Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
			Debug.Assert(count <= m_Buffer.Length - m_Position, $"参数{nameof(count)}不能超过可读取数据的长度");
			return m_Buffer[m_Position..(m_Position + count)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReadOnlyMemory<T> GetMemory(int count) {
			Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
			Debug.Assert(count <= m_Buffer.Length - m_Position, $"参数{nameof(count)}不能超过可读取数据的长度");
			return m_Buffer[m_Position..(m_Position + count)];
		}

		public void Dispose() { }
	}
}