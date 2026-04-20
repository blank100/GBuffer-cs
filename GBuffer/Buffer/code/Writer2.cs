using System.Buffers;
using System.Runtime.InteropServices;

namespace Gal.Core {
    /// <summary>
    /// Writer2
    /// </summary>
    /// <author>gouanlin</author>
    /// <typeparam name="T"></typeparam>
    public class Writer2<T> : IWriter2<Writer2<T>, T>, IBufferWriter<T> {
        //默认容量
        public const int DEFAULT_CAPACITY = 256;

        protected T[] Buffer;
        protected int _position;
        protected int _length;

        /// <summary>
        /// 长度
        /// </summary>
        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
            set {
                Debug.Assert(value >= 0, $"{nameof(Length)} cannot be less than 0");

                _length = value;

                if (value > Buffer.Length) GrowBuffer(value - Buffer.Length);
                else if (value < _position) _position = value;
            }
        }

        /// <summary>
        /// 当前位置
        /// </summary>
        public int Position {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                Debug.Assert(0 <= value && value <= Buffer.Length, $"{nameof(Position)} cannot be less than 0 or greater than {nameof(Length)}");
                _position = value;
            }
        }

        /// <summary>
        /// 容量
        /// </summary>
        public int Capacity {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer.Length;
        }

        /// <summary>
        /// 长度减去当前位置
        /// </summary>
        public int WritableCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer.Length - _position;
        }

        public Memory<T> WrittenMemory {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer.AsMemory(0, _length);
        }

        public Memory<T> Memory {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer.AsMemory(_position);
        }

        public Span<T> WrittenSpan {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer.AsSpan(0, _length);
        }

        public Span<T> Span {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer.AsSpan(_position);
        }

        public T[] RawArray {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Buffer;
        }

        public Writer2(int capacity = DEFAULT_CAPACITY) {
            Debug.Assert(capacity >= 0, $"The parameter {nameof(capacity)} cannot be negative");

            Buffer = ArrayPool<T>.Shared.Rent(capacity);
            _position = 0;
            _length = 0;
        }

        public T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Buffer[index] = value;
        }

        /// <summary>
        /// 在当前位置写入一个元素,并将 position 向后移动1位
        /// </summary>
        /// <param name="element"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Writer2<T> Write(T element) {
            var p = _position;
            var n = p + 1;

            if (n > Buffer.Length) GrowBuffer(1);

            Unsafe.Add(ref MemoryMarshal.GetReference<T>(Buffer), p) = element;

            _position = n;
            if (n > _length) _length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入两个元素,并将 position 向后移动2位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Writer2<T> Write(T element1, T element2) {
            var p = _position;
            HintSize(2);

            ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference<T>(Buffer), p);
            Unsafe.Add(ref b, 0) = element1;
            Unsafe.Add(ref b, 1) = element2;

            var n = p + 2;
            _position = n;
            if (n > _length) _length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入三个元素,并将 position 向后移动3位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        /// <param name="element3"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Writer2<T> Write(T element1, T element2, T element3) {
            var p = _position;
            HintSize(3);

            ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference<T>(Buffer), p);
            Unsafe.Add(ref b, 0) = element1;
            Unsafe.Add(ref b, 1) = element2;
            Unsafe.Add(ref b, 2) = element3;

            var n = p + 3;
            _position = n;
            if (n > _length) _length = n;

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
        public Writer2<T> Write(T element1, T element2, T element3, T element4) {
            var p = _position;
            HintSize(4);

            ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference<T>(Buffer), p);
            Unsafe.Add(ref b, 0) = element1;
            Unsafe.Add(ref b, 1) = element2;
            Unsafe.Add(ref b, 2) = element3;
            Unsafe.Add(ref b, 3) = element4;

            var n = p + 4;
            _position = n;
            if (n > _length) _length = n;

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
        public Writer2<T> Write(T element1, T element2, T element3, T element4, T element5) {
            var p = _position;
            HintSize(5);

            ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference<T>(Buffer), p);
            Unsafe.Add(ref b, 0) = element1;
            Unsafe.Add(ref b, 1) = element2;
            Unsafe.Add(ref b, 2) = element3;
            Unsafe.Add(ref b, 3) = element4;
            Unsafe.Add(ref b, 4) = element5;

            var n = p + 5;
            _position = n;
            if (n > _length) _length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入三个元素,并将 position 向后移动6位
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        /// <param name="element3"></param>
        /// <param name="element4"></param>
        /// <param name="element5"></param>
        /// <param name="element6"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Writer2<T> Write(T element1, T element2, T element3, T element4, T element5, T element6) {
            var p = _position;
            HintSize(6);

            ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference<T>(Buffer), p);
            Unsafe.Add(ref b, 0) = element1;
            Unsafe.Add(ref b, 1) = element2;
            Unsafe.Add(ref b, 2) = element3;
            Unsafe.Add(ref b, 3) = element4;
            Unsafe.Add(ref b, 4) = element5;
            Unsafe.Add(ref b, 5) = element6;

            var n = p + 6;
            _position = n;
            if (n > _length) _length = n;

            return this;
        }

        /// <summary>
        /// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
        /// </summary>
        /// <param name="elements"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Writer2<T> Write(ReadOnlySpan<T> elements) {
            var count = elements.Length;
            if (count == 0) return this;
            HintSize(count);
            elements.CopyTo(Buffer.AsSpan(_position));
            Advance(count);

            return this;
        }

        /// <summary>
        /// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
        /// </summary>
        /// <param name="elements"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Writer2<T> Write(ReadOnlyMemory<T> elements) {
            var count = elements.Length;
            if (count == 0) return this;
            HintSize(count);
            elements.Span.CopyTo(Buffer.AsSpan(_position));
            Advance(count);

            return this;
        }

        /// <summary>
        /// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
        /// </summary>
        /// <param name="elements"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Writer2<T> Write(ReadOnlySequence<T> elements) {
            var count = (int)elements.Length;
            if (count == 0) return this;
            HintSize(count);
            elements.CopyTo(Buffer.AsSpan(_position));
            Advance(count);

            return this;
        }

        /// <summary>
        /// 生成新的 buffer ,并回收原 buffer
        /// </summary>
        /// <param name="growSize">增长的长度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GrowBuffer(int growSize) {
            var len = Buffer.Length;
            GenerateBuffer(checked(len + (growSize > len ? growSize : len)));
        }

        /// <summary>
        /// 生成新的 buffer ,并回收原 buffer
        /// </summary>
        /// <param name="size"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateBuffer(int size) {
            var buffer = ArrayPool<T>.Shared.Rent(size);
            Buffer.AsSpan(0, Math.Min(_length, Buffer.Length)).CopyTo(buffer);
            ArrayPool<T>.Shared.Return(Buffer, !typeof(T).IsValueType);
            Buffer = buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count) {
            Debug.Assert(_position + count >= 0, $"移动后的指针位置不能未负数");
            Debug.Assert(_position + count <= Buffer.Length, "移动后的指针位置超出了buffer的容量");

            var t = _position += count;
            if (_length < t) _length = t;
        }

        /// <summary>
        /// 清理
        /// <para>不会真实的清理所有元素,只是将 position 和 length 置为 0 </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Writer2<T> Clear() {
            Length = 0;
            return this;
        }

        public Writer2<T> Discard() {
            if (_position <= 0) return this;
            var l = _length - _position;
            if (l > 0) {
                Buffer.AsSpan(_position, l).CopyTo(Buffer.AsSpan(0));
                _position = 0;
                _length = l;
            } else Clear();

            return this;
        }

        /// <summary>
        /// 获取 span
        /// </summary>
        /// <param name="sizeHint">需要的 span 的长度, 不足则会扩充 buffer 到足够长度, 此参数为 0 , 则返回当前位置到 capacity 的 span </param>
        /// <returns></returns>
        public Span<T> GetSpan(int sizeHint = 0) {
            Debug.Assert(sizeHint >= 0, $"The parameter of {nameof(sizeHint)} cannot be negative");
            if (sizeHint == 0) return Buffer.AsSpan(_position);
            HintSize(sizeHint);
            return Buffer.AsSpan(_position);
        }

        /// <summary>
        /// 获取 memory
        /// </summary>
        /// <param name="sizeHint">需要的 memory 的长度, 不足则会扩充 buffer 到足够长度, 此参数为 0 , 则返回当前位置到 capacity 的 memory </param>
        /// <returns></returns>
        public Memory<T> GetMemory(int sizeHint = 0) {
            Debug.Assert(sizeHint >= 0, $"The parameter of {nameof(sizeHint)} cannot be negative");

            if (sizeHint == 0) return Buffer.AsMemory(_position);
            HintSize(sizeHint);
            return Buffer.AsMemory(_position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void HintSize(int sizeHint) {
            Debug.Assert(sizeHint > 0, $"The parameter of {nameof(sizeHint)} must be greater than 0");

            var availableSize = Buffer.Length - _position;
            if (availableSize >= sizeHint) return;
            GrowBuffer(sizeHint - availableSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
            ArrayPool<T>.Shared.Return(Buffer, !typeof(T).IsValueType);
            Buffer = null;
        }
    }
}
