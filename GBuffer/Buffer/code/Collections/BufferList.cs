#nullable enable

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Gal.Core {
    /// <summary>
    /// 轻量 Span/ArrayPool 支持的临时列表。
    /// - initialCapacity=0 时懒分配
    /// - 可用 stackalloc/外部 Span 做 0 分配模式
    /// - netstandard2.1 compatible
    /// </summary>
    public ref struct BufferList<T> {
        private const int MinimumCapacity = 32;

        private static readonly bool s_containsRefs = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private T[]? _array;
        private Span<T> _span;
        private int _count;
        private bool _disposed;

        /// <summary> 基于容量的构造。传入 0 将进行懒加载分配。 </summary>
        public BufferList(int initialCapacity = MinimumCapacity) {
            if (initialCapacity <= 0) initialCapacity = MinimumCapacity;
            _array = ArrayPool<T>.Shared.Rent(initialCapacity);
            _span = _array;

            _count = 0;
            _disposed = false;
        }

        /// <summary> 基于外部栈内存 (stackalloc) 或既有 Span 的构造。0 分配。 </summary>
        public BufferList(Span<T> initialBuffer) {
            _array = null;
            _span = initialBuffer;
            _count = 0;
            _disposed = false;
        }

        public BufferList(IEnumerable<T> source) {
            switch (source) {
                case null: throw new ArgumentNullException(nameof(source));
                case ICollection<T> collection:
                    _array = ArrayPool<T>.Shared.Rent(Math.Max(collection.Count, MinimumCapacity));
                    collection.CopyTo(_array, 0);
                    _count = collection.Count;
                    _span = _array;
                    _disposed = false;
                    break;
                default: {
                    _array = ArrayPool<T>.Shared.Rent( MinimumCapacity);
                    _count = 0;
                    _span = _array;
                    _disposed = false;
                    foreach (var item in source) Add(item);
                    break;
                }
            }
        }

        public readonly int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }

        public readonly int Capacity {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span.Length;
        }

        // 使用属性拦截调用。只有在调用方的编译符号包含 DEBUG 或 BUFFERLIST_CHECKS 时，调用才会被编译进去。
        [Conditional("DEBUG")]
        [Conditional("BUFFERLIST_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckDisposed() {
            if (_disposed) throw new ObjectDisposedException(nameof(BufferList<T>));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan() {
            CheckDisposed();
            return _span[.._count];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsReadOnlySpan() {
            CheckDisposed();
            return _span[.._count];
        }

        public readonly ref T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                CheckDisposed();
                if ((uint)index >= (uint)_count) ThrowIndexOutOfRange();
                return ref _span[index];
            }
        }

        public readonly ref T this[Index index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                CheckDisposed();
                var offset = index.GetOffset(_count);
                if ((uint)offset >= (uint)_count) ThrowIndexOutOfRange();
                return ref _span[offset];
            }
        }

        public readonly Span<T> this[Range range] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                CheckDisposed();
                var (offset, length) = range.GetOffsetAndLength(_count);
                return _span.Slice(offset, length);
            }
        }

        public void Dispose() {
            if (_disposed) return;

            ReturnArray();
            _array = null;

            _span = default;
            _count = 0;
            _disposed = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReturnArray() {
            if (_array == null) return;
            if (s_containsRefs) _span[.._count].Clear();
            ArrayPool<T>.Shared.Return(_array, clearArray: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow() {
            var newSize = _span.Length == 0 ? 4 : (_span.Length * 2);
            GrowTo(newSize);
        }

        public void EnsureCapacity(int capacity) {
            CheckDisposed();
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (capacity <= _span.Length) return;

            var doubled = _span.Length == 0 ? 4 : (_span.Length * 2);
            var newCap = Math.Max(doubled, capacity);
            GrowTo(newCap);
        }

        private void GrowTo(int newSize) {
            CheckDisposed();
            if (newSize < 0) throw new ArgumentOutOfRangeException(nameof(newSize));

            var newArr = ArrayPool<T>.Shared.Rent(newSize);

            _span[.._count].CopyTo(newArr);

            ReturnArray();

            _array = newArr;
            _span = newArr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T value) {
            CheckDisposed();
            if (_count == _span.Length) Grow();
            _span[_count++] = value;
        }

        /// <summary>
        /// 不做容量检查（仅 Debug.Assert）。调用方需保证 Capacity 足够。
        /// in T：避免大 struct 传值拷贝。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeAdd(in T value) {
            CheckDisposed();
            Debug.Assert(_count < _span.Length);
            _span[_count++] = value;
        }

        public void AddRange(ReadOnlySpan<T> values) {
            CheckDisposed();

            int required;
            checked {
                required = _count + values.Length;
            }

            EnsureCapacity(required);
            values.CopyTo(_span[_count..]);
            _count = required;
        }

        public void Insert(int index, T value) {
            CheckDisposed();
            if ((uint)index > (uint)_count) ThrowArgumentOutOfRange_Index();

            if (_count == _span.Length) Grow();

            var moveCount = _count - index;
            if (moveCount > 0) _span.Slice(index, moveCount).CopyTo(_span[(index + 1)..]);

            _span[index] = value;
            _count++;
        }

        public T RemoveAt(int index) {
            CheckDisposed();
            if ((uint)index >= (uint)_count) ThrowArgumentOutOfRange_Index();

            var removed = _span[index];

            var tail = _count - index - 1;
            if (tail > 0) _span.Slice(index + 1, tail).CopyTo(_span.Slice(index, tail));

            _count--;
            if (s_containsRefs) _span[_count] = default!;

            return removed;
        }

        public bool Remove(T value) {
            CheckDisposed();
            var index = IndexOf(value);
            if (index >= 0) {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public T? Find(Predicate<T> match) {
            CheckDisposed();
            if (match == null) throw new ArgumentNullException(nameof(match));

            var span = _span[.._count];
            ref var r0 = ref MemoryMarshal.GetReference(span);
            for (var i = 0; i < span.Length; i++) {
                var item = Unsafe.Add(ref r0, i);
                if (match(item)) return item;
            }

            return default;
        }

        public bool TryFind(Predicate<T> match, [MaybeNullWhen(false)] out T value) {
            CheckDisposed();
            if (match == null) throw new ArgumentNullException(nameof(match));

            var span = _span[.._count];
            ref var r0 = ref MemoryMarshal.GetReference(span);
            for (var i = 0; i < span.Length; i++) {
                var item = Unsafe.Add(ref r0, i);
                if (match(item)) {
                    value = item;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public int FindIndex(Predicate<T> match) {
            CheckDisposed();
            if (match == null) throw new ArgumentNullException(nameof(match));

            var span = _span[.._count];
            ref var r0 = ref MemoryMarshal.GetReference(span);
            for (var i = 0; i < span.Length; i++) {
                if (match(Unsafe.Add(ref r0, i))) return i;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOf(T value) {
            CheckDisposed();
            var comparer = EqualityComparer<T>.Default;

            var span = _span[.._count];
            ref var r0 = ref MemoryMarshal.GetReference(span);
            for (var i = 0; i < span.Length; i++) {
                if (comparer.Equals(Unsafe.Add(ref r0, i), value)) return i;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(T value) =>
            // CheckDisposed 在 IndexOf 内部已调用
            IndexOf(value) >= 0;

        public void Reverse() {
            CheckDisposed();
            _span[.._count].Reverse();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            CheckDisposed();
            if (s_containsRefs) _span[.._count].Clear();

            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<T> destination) {
            CheckDisposed();
            if (destination.Length < _count) throw new ArgumentException("Destination span is too small.", nameof(destination));
            _span[.._count].CopyTo(destination);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T>.Enumerator GetEnumerator() {
            CheckDisposed();
            return AsSpan().GetEnumerator();
        }

        // 把 Throw 逻辑提出来，防止影响高频属性 AggressiveInlining 的命中率
        [DoesNotReturn]
        private static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();

        [DoesNotReturn]
        private static void ThrowArgumentOutOfRange_Index() => throw new ArgumentOutOfRangeException("index");
    }
}
