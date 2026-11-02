using System.Buffers;

namespace Gal.Core
{
    /// <summary>
    ///
    /// </summary>
    /// <author>gouanlin</author>
    public sealed class Buffer<T> : Writer<T>, IBuffer<T>
	{
		public Buffer(int capacity = DEFAULT_CAPACITY) : base(capacity) { }

		// IBuffer<T> 声明了 new 双向 indexer，这里提供公开实现（基类只有 set）
		public new T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Buffer[index];
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Buffer[index] = value;
		}

		public int ReadableCount {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _length - _position;
		}

		ReadOnlyMemory<T> IReader<T>.Memory {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Memory; // 与基类 Writer<T>.Memory 相同（Buffer.AsMemory(_position)）
		}

		ReadOnlySpan<T> IReader<T>.Span {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Span; // 与基类 Writer<T>.Span 相同（Buffer.AsSpan(_position)）
		}

		T IReader<T>.this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Buffer[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Read() {
			if ((uint)_position >= (uint)Buffer.Length) throw new System.IO.EndOfStreamException("No more data to read.");
			return Buffer[_position++];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ReadOnlySpan<T> IReader<T>.GetSpan(int count) {
			Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
			Debug.Assert(count <= Buffer.Length - _position, $"参数{nameof(count)}不能超过可读取数据的长度");
			return Buffer.AsSpan(_position, count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		ReadOnlyMemory<T> IReader<T>.GetMemory(int count) {
			Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
			Debug.Assert(count <= Buffer.Length - _position, $"参数{nameof(count)}不能超过可读取数据的长度");
			return Buffer.AsMemory(_position, count);
		}
	}
}
