using System.Buffers;

namespace Gal.Core
{
    /// <summary>
    ///
    /// </summary>
    /// <author>gouanlin</author>
    public sealed class Buffer<T> : Writer<T>, IBuffer<T>, IWriter<Buffer<T>, T>
	{
		public Buffer(int capacity = DEFAULT_CAPACITY) : base(capacity) { }

		// IBuffer<T> 声明了 new 双向 indexer，这里提供公开实现（基类只有 set）
		public new T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Buffer[index];
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Buffer[index] = value;
		}

		// CRTP 强类型链式：以下 new 方法隐藏基类的 IWriter<T> 返回，返回具体类型 Buffer<T>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new Buffer<T> Write(T element) { base.Write(element); return this; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new Buffer<T> Write(T element1, T element2) { base.Write(element1, element2); return this; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new Buffer<T> Write(T element1, T element2, T element3) { base.Write(element1, element2, element3); return this; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new Buffer<T> Write(ReadOnlySpan<T> elements) { base.Write(elements); return this; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new Buffer<T> Write(ReadOnlyMemory<T> elements) { base.Write(elements); return this; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new Buffer<T> Write(ReadOnlySequence<T> elements) { base.Write(elements); return this; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new Buffer<T> Clear() { base.Clear(); return this; }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new Buffer<T> Discard() { base.Discard(); return this; }
		// IBuffer<T> 继承 IWriter<IBuffer<T>, T>，此处显式转发到具体方法（返回 Buffer<T> 隐式转 IBuffer<T>）
		IBuffer<T> IWriter<IBuffer<T>, T>.Write(T element) => Write(element);
		IBuffer<T> IWriter<IBuffer<T>, T>.Write(T element1, T element2) => Write(element1, element2);
		IBuffer<T> IWriter<IBuffer<T>, T>.Write(T element1, T element2, T element3) => Write(element1, element2, element3);
		IBuffer<T> IWriter<IBuffer<T>, T>.Write(ReadOnlySpan<T> elements) => Write(elements);
		IBuffer<T> IWriter<IBuffer<T>, T>.Write(ReadOnlyMemory<T> elements) => Write(elements);
		IBuffer<T> IWriter<IBuffer<T>, T>.Write(ReadOnlySequence<T> elements) => Write(elements);
		IBuffer<T> IWriter<IBuffer<T>, T>.Clear() => Clear();
		IBuffer<T> IWriter<IBuffer<T>, T>.Discard() => Discard();

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
