using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gal.Core
{
    /// <summary>
    /// 栈上的 Stack
    /// </summary>
    /// <author>gouanlin</author>
    public ref struct RefStack<T>
    {
        private T[] _buffer;
        private Span<T> _span;
        private int _position;

        public int Count => _position;

        public RefStack(Span<T> buffer) {
            _buffer = default;
            _span = buffer;
            _position = 0;
        }

        public RefStack(int capacity) {
            Debug.Assert(capacity >= 0, $"The parameter {nameof(capacity)} cannot be negative");

            _buffer = ArrayPool<T>.Shared.Rent(capacity);
            _span = _buffer;
            _position = 0;
        }

        /// <summary>
        /// 在当前位置写入一个元素,并将 position 向后移动1位
        /// </summary>
        /// <param name="element"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T element) {
            if (_position + 1 > _span.Length) GrowBuffer(1);
            _span[_position++] = element;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop() => _span[--_position];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek() => _span[_position - 1];

        /// <summary>
        /// 增长 buffer
        /// </summary>
        /// <param name="growSize">增长的长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GrowBuffer(int growSize) {
            var len = _span.Length;
            GenerateBuffer(checked(len + Math.Max(growSize, len)));
        }

        /// <summary>
        /// 生成新的 buffer ,并回收原 buffer
        /// </summary>
        /// <param name="size"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateBuffer(int size) {
            var buffer = ArrayPool<T>.Shared.Rent(size);
            _span[.._position].CopyTo(buffer);
            if (_buffer != null) ArrayPool<T>.Shared.Return(_buffer, !typeof(T).IsValueType);
            _span = _buffer = buffer;
        }

        /// <summary>
        /// 清理
        /// <para>不会真实的清理所有元素,只是将 position 和 length 置为 0 </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _position = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
            var buffer = _buffer;
            this = default;
            if (buffer != null) ArrayPool<T>.Shared.Return(buffer, !typeof(T).IsValueType);
        }
    }
}
