namespace Gal.Core
{
    /// <summary>
    ///
    /// </summary>
    /// <author>gouanlin</author>
    public sealed class Buffer2<T> : Writer2<Buffer2<T>,T>, IBuffer2<Buffer2<T>, T>
	{
        public Buffer2(int capacity = DEFAULT_CAPACITY) : base(capacity) { }

        public int ReadableCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length - _position;
        }

        ReadOnlyMemory<T> IReader<T>.Memory {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer.AsMemory(_position);
        }

        ReadOnlySpan<T> IReader<T>.Span {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer.AsSpan(_position);
        }

        T IReader<T>.this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read() => Buffer[_position++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ReadOnlySpan<T> IReader<T>.GetSpan(int count) {
            Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
            Debug.Assert(count <= Buffer.Length - _position, $"参数{nameof(count)}不能超过可读取数据的长度");
            return Buffer[_position..(_position + count)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ReadOnlyMemory<T> IReader<T>.GetMemory(int count) {
            Debug.Assert(count >= 0, $"参数{nameof(count)}不能为负数");
            Debug.Assert(count <= Buffer.Length - _position, $"参数{nameof(count)}不能超过可读取数据的长度");
            return Buffer[_position..(_position + count)];
        }
	}
}
