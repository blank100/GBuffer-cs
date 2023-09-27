using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gal.Core
{
	/// <summary>
	/// 栈上的reader
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <para>author gouanlin</para>
	public ref struct RefReader<T>
	{
		private readonly ReadOnlySpan<T> m_Span;
		private int m_Position;

		public int length {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Span.Length;
		}

		public int position {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Position;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				Debug.Assert(0 <= value && value <= m_Span.Length, $"{nameof(position)} cannot be less than 0 or greater than {nameof(length)}");
				m_Position = value;
			}
		}

		public int readableCount {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Span.Length - m_Position;
		}

		public ReadOnlySpan<T> span {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Span[m_Position..];
		}

		public RefReader(ReadOnlySpan<T> original) {
			m_Span = original;
			m_Position = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Read() => m_Span[m_Position++];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Advance(int count) {
			Debug.Assert(count >= 0, $"The parameter of {nameof(Advance)} cannot be negative");

			m_Position += count;
		}

		public ReadOnlySpan<T> GetSpan(int count) {
			Debug.Assert(count <= m_Span.Length - m_Position, $"The parameter {nameof(count)} cannot be negative");

			return m_Span.Slice(m_Position, count);
		}

		public void Dispose() {
			this = default;
		}
	}
}