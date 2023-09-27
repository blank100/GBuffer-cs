using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gal.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <para>author gouanlin</para>
    public sealed class Buffer<T> : Writer<T>, IBuffer<T>
	{
		public Buffer(int capacity = DEFAULT_CAPACITY) : base(capacity) { }

		public int readableCount {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Length - m_Position;
		}

		ReadOnlyMemory<T> IReader<T>.memory {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Buffer.AsMemory(m_Position);
		}

		ReadOnlySpan<T> IReader<T>.span {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Buffer.AsSpan(m_Position);
		}

		T IReader<T>.this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Buffer[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Read() {
			return m_Buffer[m_Position++];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ReadOnlySpan<T> IReader<T>.GetSpan(int count) {
			Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
			Debug.Assert(count <= m_Buffer.Length - m_Position, $"参数{nameof(count)}不能超过可读取数据的长度");
			return m_Buffer[m_Position..(m_Position + count)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ReadOnlyMemory<T> IReader<T>.GetMemory(int count) {
			Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
			Debug.Assert(count <= m_Buffer.Length - m_Position, $"参数{nameof(count)}不能超过可读取数据的长度");
			return m_Buffer[m_Position..(m_Position + count)];
		}
	}
}