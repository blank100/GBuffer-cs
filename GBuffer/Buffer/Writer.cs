using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Gal.Core
{
    /// <summary>
    /// Writer 
    /// </summary>
    /// <para>author gouanlin</para>
    /// <typeparam name="T"></typeparam>
    public class Writer<T> : IWriter<T>, IBufferWriter<T>
    {
        //默认容量
        public const int DEFAULT_CAPACITY = 256;

        protected T[] m_Buffer;
        protected int m_Position;
        protected int m_Length;

        /// <summary>
        /// 长度
        /// </summary>
        public int length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Length;
            set {
                Debug.Assert(value >= 0, $"{nameof(length)} cannot be less than 0");

                m_Length = value;

                if (value > m_Buffer.Length) {
                    GrowBuffer(value - m_Buffer.Length);
                } else if (value < m_Position) {
                    m_Position = value;
                }
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
            get => m_Buffer.Length;
        }

        /// <summary>
        /// 长度减去当前位置
        /// </summary>
        public int writableCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer.Length - m_Position;
        }

        /// <summary>
        /// 获取已写入数据的 memory
        /// <para>即从 0 到 length 的 memory</para>
        /// </summary>
        public Memory<T> writtenMemory {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer.AsMemory(0, m_Length);
        }

        /// <summary>
        /// 获取 memory
        /// <para>即当前位置到 capacity 的 memory</para>
        /// </summary>
        public Memory<T> memory {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer.AsMemory(m_Position);
        }

        /// <summary>
        /// 获取已写入数据的 span
        /// <para>即从 0 到 length 的 span</para>
        /// </summary>
        public Span<T> writtenSpan {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer.AsSpan(0, m_Length);
        }

        /// <summary>
        /// 获取 span
        /// <para>即当前位置到 capacity 的 span</para>
        /// </summary>
        public Span<T> span {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer.AsSpan(m_Position);
        }

        public T[] rawArray {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer;
        }

        public Writer(int capacity = DEFAULT_CAPACITY) {
            Debug.Assert(capacity >= 0, $"The parameter {nameof(capacity)} cannot be negative");

            m_Buffer = ArrayPool<T>.Shared.Rent(capacity);
            m_Position = 0;
            m_Length = 0;
        }

        public T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_Buffer[index] = value;
        }

        /// <summary>
        /// 在当前位置写入一个元素,并将 position 向后移动1位
        /// </summary>
        /// <param name="element"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Write(T element) {
            var t = m_Buffer;
            var p = m_Position;
            var n = p + 1;
            if (n > t.Length) {
                GrowBuffer(1);
                t = m_Buffer;
            }

            t[p] = element;
            m_Position = n;
            if (m_Length < n) m_Length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入两个元素,并将 position 向后移动2位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Write(T element1, T element2) {
            var t = m_Buffer;
            var p = m_Position;
            var n = p + 2;
            if (n > t.Length) {
                GrowBuffer(2);
                t = m_Buffer;
            }

            t[p] = element1;
            t[p + 1] = element2;

            m_Position = n;
            if (m_Length < n) m_Length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入三个元素,并将 position 向后移动3位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        /// <param name="element3"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Write(T element1, T element2, T element3) {
            var t = m_Buffer;
            var p = m_Position;
            var n = p + 3;
            if (n > t.Length) {
                GrowBuffer(3);
                t = m_Buffer;
            }

            t[p] = element1;
            t[p + 1] = element2;
            t[p + 2] = element3;

            m_Position = n;
            if (m_Length < n) m_Length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入三个元素,并将 position 向后移动4位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        /// <param name="element3"></param>
        /// <param name="element4"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Write(T element1, T element2, T element3, T element4) {
            var t = m_Buffer;
            var p = m_Position;
            var n = p + 4;
            if (n > t.Length) {
                GrowBuffer(4);
                t = m_Buffer;
            }

            t[p] = element1;
            t[p + 1] = element2;
            t[p + 2] = element3;
            t[p + 3] = element4;

            m_Position = n;
            if (m_Length < n) m_Length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入三个元素,并将 position 向后移动5位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        /// <param name="element3"></param>
        /// <param name="element4"></param>
        /// <param name="element5"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Write(T element1, T element2, T element3, T element4, T element5) {
            var t = m_Buffer;
            var p = m_Position;
            var n = p + 5;
            if (n > t.Length) {
                GrowBuffer(5);
                t = m_Buffer;
            }

            t[p] = element1;
            t[p + 1] = element2;
            t[p + 2] = element3;
            t[p + 3] = element4;
            t[p + 4] = element5;

            m_Position = n;
            if (m_Length < n) m_Length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入三个元素,并将 position 向后移动5位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        /// <param name="element3"></param>
        /// <param name="element4"></param>
        /// <param name="element5"></param>
        /// <param name="element6"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Write(T element1, T element2, T element3, T element4, T element5, T element6) {
            var t = m_Buffer;
            var p = m_Position;
            var n = p + 6;
            if (n > t.Length) {
                GrowBuffer(6);
                t = m_Buffer;
            }

            t[p] = element1;
            t[p + 1] = element2;
            t[p + 2] = element3;
            t[p + 3] = element4;
            t[p + 4] = element5;
            t[p + 5] = element6;

            m_Position = n;
            if (m_Length < n) m_Length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
        /// </summary>
        /// <param name="elements"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Write(ReadOnlySpan<T> elements) {
            var count = elements.Length;

            var t = m_Buffer;
            var p = m_Position;
            var n = p + count;
            if (n > t.Length) {
                GrowBuffer(count);
                t = m_Buffer;
            }
            elements.CopyTo(t.AsSpan(p));
            m_Position = n;
            if (m_Length < n) m_Length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
        /// </summary>
        /// <param name="elements"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Write(ReadOnlyMemory<T> elements) {
            var count = elements.Length;

            var t = m_Buffer;
            var p = m_Position;
            var n = p + count;
            if (n > t.Length) {
                GrowBuffer(count);
                t = m_Buffer;
            }

            elements.Span.CopyTo(t.AsSpan(p));

            m_Position = n;
            if (m_Length < n) m_Length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
        /// </summary>
        /// <param name="elements"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Write(ReadOnlySequence<T> elements) {
            var count = (int)elements.Length;

            var t = m_Buffer;
            var p = m_Position;
            var n = p + count;
            if (n > t.Length) {
                GrowBuffer(count);
                t = m_Buffer;
            }

            elements.CopyTo(t.AsSpan(p));

            m_Position = n;
            if (m_Length < n) m_Length = n;

            return this;
        }

        /// <summary>
        /// 生成新的 buffer ,并回收原 buffer
        /// </summary>
        /// <param name="growSize">增长的长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GrowBuffer(int growSize) {
            var len = m_Buffer.Length;
            GenerateBuffer(checked(len + (growSize > len ? growSize : len)));
        }

        /// <summary>
        /// 生成新的 buffer ,并回收原 buffer
        /// </summary>
        /// <param name="size"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateBuffer(int size) {
            var buffer = ArrayPool<T>.Shared.Rent(size);
            m_Buffer.AsSpan(0, m_Length).CopyTo(buffer);
            ArrayPool<T>.Shared.Return(m_Buffer);
            m_Buffer = buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count) {
            Debug.Assert(m_Position + count >= 0, $"移动后的指针位置不能未负数");
            Debug.Assert(m_Position + count <= m_Buffer.Length, "移动后的指针位置超出了buffer的容量");

            var t = m_Position += count;
            if (m_Length < t) m_Length = t;
        }

        /// <summary>
        /// 清理
        /// <para>不会真实的清理所有元素,只是将 position 和 length 置为 0 </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IWriter<T> Clear() {
            length = 0;
            return this;
        }

        public IWriter<T> Discard() {
            if (m_Position <= 0) return this;
            var l = m_Length - m_Position;
            if (l > 0) {
                m_Buffer.AsSpan(m_Position, l).CopyTo(m_Buffer.AsSpan(0));
                m_Position = 0;
                m_Length = l;
            } else Clear();
            return this;
        }

        public Span<T> GetSpan(int sizeHint = 0) {
            Debug.Assert(sizeHint >= 0, $"The parameter of {nameof(sizeHint)} cannot be negative");
            if (sizeHint == 0) return m_Buffer.AsSpan(m_Position);
            var availableSize = m_Buffer.Length - m_Position;
            if (availableSize < sizeHint) GrowBuffer(sizeHint - availableSize);
            return m_Buffer.AsSpan(m_Position, sizeHint);
        }

        public Memory<T> GetMemory(int sizeHint = 0) {
            Debug.Assert(sizeHint >= 0, $"The parameter of {nameof(sizeHint)} cannot be negative");

            if (sizeHint == 0) return m_Buffer.AsMemory(m_Position);
            var availableSize = m_Buffer.Length - m_Position;
            if (availableSize < sizeHint) GrowBuffer(sizeHint - availableSize);
            return m_Buffer.AsMemory(m_Position, sizeHint);
        }

        public void HintSize(int sizeHint) {
            Debug.Assert(sizeHint > 0, $"The parameter of {nameof(sizeHint)} must be greater than 0");

            var availableSize = m_Buffer.Length - m_Position;
            if (availableSize >= sizeHint) return;
            GrowBuffer(sizeHint - availableSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
            ArrayPool<T>.Shared.Return(m_Buffer);
            m_Buffer = null;
        }
    }
}