using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Gal.Core
{
    /// <summary>
    /// 栈上的 Writer 
    /// </summary>
    /// <para>author gouanlin</para>
    /// <typeparam name="T"></typeparam>
    public ref struct RefWriter<T>
    {
        //默认容量
        public const int DEFAULT_CAPACITY = 256;

        private T[] m_Buffer;
        private Span<T> m_Span;
        private int m_Position;
        private int m_Length;

        /// <summary>
        /// 长度
        /// </summary>
        public int length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Length;
            set {
                Debug.Assert(value >= 0, $"{nameof(length)} cannot be less than 0");

                m_Length = value;
                if (value > m_Span.Length) GrowBuffer(value - m_Span.Length);
                else if (value < m_Position) m_Position = value;
            }
        }

        /// <summary>
        /// 当前位置
        /// </summary>
        public int position {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                Debug.Assert(0 <= value && value <= m_Buffer.Length, $"{nameof(position)} cannot be less than 0 or greater than {nameof(length)}");

                m_Position = value;
            }
        }

        /// <summary>
        /// 容量
        /// </summary>
        public int capacity {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Span.Length;
        }

        /// <summary>
        /// 长度减去当前位置
        /// </summary>
        public int remaining {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Length - m_Position;
        }

        /// <summary>
        /// 获取已写入数据的 memory
        /// <para>即从 0 到 length 的 memory</para>
        /// </summary>
        public Memory<T> writtenMemory {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (m_Buffer == null) GenerateBuffer(m_Span.Length);
                return m_Buffer.AsMemory(0, m_Length);
            }
        }

        /// <summary>
        /// 获取 memory
        /// <para>即当前位置到 capacity 的 memory</para>
        /// </summary>
        public Memory<T> memory {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (m_Buffer == null) GenerateBuffer(m_Span.Length);
                return m_Buffer.AsMemory(m_Position);
            }
        }

        /// <summary>
        /// 获取已写入数据的 span
        /// <para>即从 0 到 length 的 span</para>
        /// </summary>
        public Span<T> writtenSpan {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Span[..m_Length];
        }

        /// <summary>
        /// 获取 span
        /// <para>即当前位置到 capacity 的 span</para>
        /// </summary>
        public Span<T> span {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Span[m_Position..];
        }

        public RefWriter(Span<T> buffer) {
            m_Buffer = default;
            m_Span = buffer;
            m_Position = 0;
            m_Length = 0;
        }

        public RefWriter(int capacity = DEFAULT_CAPACITY) {
            Debug.Assert(capacity >= 0, $"The parameter {nameof(capacity)} cannot be negative");

            m_Buffer = ArrayPool<T>.Shared.Rent(capacity);
            m_Span = m_Buffer;
            m_Position = 0;
            m_Length = 0;
        }

        public T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Span[index];
        }

        /// <summary>
        /// 在当前位置写入一个元素,并将 position 向后移动1位
        /// </summary>
        /// <param name="element"></param>
        public void Write(T element) {
            var t = m_Span;
            var p = m_Position;
            var n = p + 1;
            if (n > t.Length) {
                GrowBuffer(1);
                t = m_Span;
            }

            t[p] = element;

            m_Position = n;
            if (m_Length < n) m_Length = n;
        }

        /// <summary>
        /// 在当前位置写入两个元素,并将 position 向后移动2位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        public void Write(T element1, T element2) {
            var t = m_Span;
            var p = m_Position;
            var n = p + 2;
            if (n > t.Length) {
                GrowBuffer(2);
                t = m_Span;
            }

            t[p] = element1;
            t[p + 1] = element2;

            m_Position = n;
            if (m_Length < n) m_Length = n;
        }

        /// <summary>
        /// 在当前位置写入三个元素,并将 position 向后移动3位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        /// <param name="element3"></param>
        public void Write(T element1, T element2, T element3) {
            var t = m_Span;
            var p = m_Position;
            var n = p + 3;
            if (n > t.Length) {
                GrowBuffer(3);
                t = m_Span;
            }

            t[p] = element1;
            t[p + 1] = element2;
            t[p + 2] = element3;

            m_Position = n;
            if (m_Length < n) m_Length = n;
        }

        /// <summary>
        /// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
        /// </summary>
        /// <param name="elements"></param>
        public void Write(ReadOnlySpan<T> elements) {
            var count = elements.Length;

            var t = m_Span;
            var p = m_Position;
            var n = p + count;
            if (n > t.Length) {
                GrowBuffer(count);
                t = m_Span;
            }
            elements.CopyTo(t[p..]);
            m_Position = n;
            if (m_Length < n) m_Length = n;
        }

        /// <summary>
        /// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
        /// </summary>
        /// <param name="elements"></param>
        public void Write(ReadOnlyMemory<T> elements) {
            var count = elements.Length;

            var t = m_Span;
            var p = m_Position;
            var n = p + count;
            if (n > t.Length) {
                GrowBuffer(count);
                t = m_Span;
            }

            elements.Span.CopyTo(t[p..]);

            m_Position = n;
            if (m_Length < n) m_Length = n;
        }

        /// <summary>
        /// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
        /// </summary>
        /// <param name="elements"></param>
        public void Write(ReadOnlySequence<T> elements) {
            var count = (int)elements.Length;

            var t = m_Span;
            var p = m_Position;
            var n = p + count;
            if (n > t.Length) {
                GrowBuffer(count);
                t = m_Span;
            }

            elements.CopyTo(t[p..]);

            m_Position = n;
            if (m_Length < n) m_Length = n;
        }

        /// <summary>
        /// 增长 buffer
        /// </summary>
        /// <param name="growSize">增长的长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GrowBuffer(int growSize) {
            var len = m_Span.Length;
            GenerateBuffer(checked(len + Math.Max(growSize, len)));
        }

        /// <summary>
        /// 生成新的 buffer ,并回收原 buffer
        /// </summary>
        /// <param name="size"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateBuffer(int size) {
            var buffer = ArrayPool<T>.Shared.Rent(size);
            m_Span[..m_Length].CopyTo(buffer);
            if (m_Buffer != null) ArrayPool<T>.Shared.Return(m_Buffer);
            m_Span = m_Buffer = buffer;
        }

        /// <summary>
        /// 指针向后移动
        /// </summary>
        /// <param name="count"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count) {
            Debug.Assert(count >= 0, $"The parameter of {nameof(count)} cannot be negative");
            Debug.Assert(m_Position + count <= m_Span.Length, "The length of the moved buffer exceeds the capacity of the buffer");

            var t = m_Position += count;
            if (m_Length < t) m_Length = t;
        }

        /// <summary>
        /// 清理
        /// <para>不会真实的清理所有元素,只是将 position 和 length 置为 0 </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => length = 0;

        /// <summary>
        /// 获取 memory
        /// </summary>
        /// <param name="sizeHint">需要的 memory 的长度, 不足则会扩充 buffer 到足够长度, 此参数为 0 , 则返回当前位置到 capacity 的 memory </param>
        /// <returns></returns>
        public Memory<T> GetMemory(int sizeHint = 0) {
            Debug.Assert(sizeHint >= 0, $"The parameter of {nameof(sizeHint)} cannot be negative");

            if (sizeHint == 0) {
                if (m_Buffer == null) GenerateBuffer(m_Span.Length);
                return m_Buffer.AsMemory(m_Position);
            }
            var availableSize = m_Span.Length - m_Position;
            if (availableSize < sizeHint) GrowBuffer(sizeHint - availableSize);
            else if (m_Buffer == null) GenerateBuffer(m_Span.Length);
            return m_Buffer.AsMemory(m_Position, sizeHint);
        }

        /// <summary>
        /// 获取 span
        /// </summary>
        /// <param name="sizeHint">需要的 memory 的长度, 不足则会扩充 buffer 到足够长度, 此参数为 0 , 则返回当前位置到 capacity 的 span </param>
        /// <returns></returns>
        public Span<T> GetSpan(int sizeHint = 0) {
            Debug.Assert(sizeHint >= 0, $"The parameter of {nameof(sizeHint)} cannot be negative");

            if (sizeHint == 0) return m_Span.Slice(m_Position);
            var availableSize = m_Span.Length - m_Position;
            if (availableSize < sizeHint) GrowBuffer(sizeHint - availableSize);
            return m_Span.Slice(m_Position, sizeHint);
        }

        /// <summary>
        /// 提示需要指定长度的空间
        /// <para>即从当前位置到 capacity 需要指定长度的空间,不足则扩充 buffer 到足够长度</para>
        /// </summary>
        /// <param name="sizeHint"></param>
        public void HintSize(int sizeHint) {
            Debug.Assert(sizeHint > 0, $"The parameter of {nameof(sizeHint)} must be greater than 0");

            var availableSize = m_Span.Length - m_Position;
            if (availableSize >= sizeHint) return;
            GrowBuffer(sizeHint - availableSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
            var buffer = m_Buffer;
            this = default;
            if (buffer != null) ArrayPool<T>.Shared.Return(buffer);
        }
    }
}