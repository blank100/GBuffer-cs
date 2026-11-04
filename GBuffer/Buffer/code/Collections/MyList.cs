#nullable enable
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Gal.Core {
    public sealed class MyList<T> : IList<T>, IList, IReadOnlyList<T> {
        private const int MinimumCapacity = 32;

        private T[] _items;
        private object? _syncRoot;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MyList() {
            _items = new T[MinimumCapacity];
            Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MyList(int capacity) {
            _items = new T[Math.Max(capacity, MinimumCapacity)];
            Count = 0;
        }

        public MyList(IEnumerable<T> source) {
            switch (source) {
                case null: throw new ArgumentNullException(nameof(source));
                case ICollection<T> collection:
                    _items = new T[Math.Max(collection.Count, MinimumCapacity)];
                    collection.CopyTo(_items, 0);
                    Count = collection.Count;
                    break;
                default: {
                    _items = new T[MinimumCapacity];
                    Count = 0;
                    foreach (var item in source) Add(item);
                    break;
                }
            }
        }

        public MyList(T[] source) {
            if (source is null) throw new ArgumentNullException(nameof(source));
            _items = new T[Math.Max(source.Length, MinimumCapacity)];
            source.CopyTo(_items, 0);
            Count = source.Length;
        }

        public MyList(ReadOnlySpan<T> source) {
            _items = new T[Math.Max(source.Length, MinimumCapacity)];
            source.CopyTo(_items);
            Count = source.Length;
        }

        public int Capacity => _items.Length;

        int ICollection.Count => Count;
        bool IList.IsFixedSize => false;
        bool ICollection.IsSynchronized => false;
        bool IList.IsReadOnly => false;

        object ICollection.SyncRoot {
            get {
                if (_syncRoot is null) Interlocked.CompareExchange<object?>(ref _syncRoot, new object(), null);
                return _syncRoot!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IList.Add(object? item) {
            if (item is not T itemAsTSource) throw new ArgumentException($"Wrong value type. Expected {typeof(T)}, got: '{item}'.", nameof(item));
            Add(itemAsTSource);
            return Count - 1;
        }

        bool IList.Contains(object? item) => item is T itemAsTSource && Contains(itemAsTSource);

        int IList.IndexOf(object? item) => item is T itemAsTSource ? IndexOf(itemAsTSource) : -1;

        void IList.Remove(object? item) {
            if (item is T itemAsTSource) Remove(itemAsTSource);
        }

        void IList.Insert(int index, object? item) {
            if (item is not T itemAsTSource) throw new ArgumentException($"Wrong value type. Expected {typeof(T)}, got: '{item}'.", nameof(item));
            Insert(index, itemAsTSource);
        }

        void ICollection.CopyTo(Array array, int arrayIndex) {
            if (array is null) throw new ArgumentNullException(nameof(array));
            if (array.Rank != 1) throw new ArgumentException("Array must be single dimensional", nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException("Destination array is not long enough.");
            Array.Copy(_items, 0, array, arrayIndex, Count);
        }

#if NETSTANDARD2_1
        [MaybeNull]
#endif
        object IList.this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
                return _items[index]!;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
                if (value is T valueAsTSource) _items[index] = valueAsTSource;
                else throw new ArgumentException($"Wrong value type. Expected {typeof(T)}, got: '{value}'.", nameof(value));
            }
        }

        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set;
        }

        public bool IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item) {
            var count = Count;
            var items = _items;
            if ((uint)count < (uint)items.Length) {
                items[count] = item;
                Count = count + 1;
            } else AddWithResize(item);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddWithResize(T item) {
            EnsureCapacity(Count + 1);
            _items[Count++] = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            if (Count == 0) return;
            // 只有引用类型需要 Clear 数组以释放 GC 引用
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) Array.Clear(_items, 0, Count);
            Count = 0;
        }

        /// 快速清空列表，不释放数组内存
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearFast() => Count = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item) => IndexOf(item) > -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item) => Array.IndexOf(_items, item, 0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_items, 0, array, arrayIndex, Count);

        public bool Remove(T item) {
            var index = IndexOf(item);
            if (index == -1) return false;
            RemoveAt(index);
            return true;
        }

        public void Insert(int index, T item) {
            if ((uint)index > (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (Count == _items.Length) EnsureCapacity(Count + 1);
            if (index < Count) Array.Copy(_items, index, _items, index + 1, Count - index);
            _items[index] = item;
            Count++;
        }

        public void RemoveAt(int index) {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            var c = --Count;
            if (index < c) Array.Copy(_items, index + 1, _items, index, c - index);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) _items[c] = default!;
        }

#if NETSTANDARD2_1
        [MaybeNull]
#endif
        public T this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
                return _items[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
                _items[index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(_items, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_items, Count);

        public void AddRange(ReadOnlySpan<T> items) {
            var newCount = Count + items.Length;
            if (newCount > _items.Length) EnsureCapacity(newCount);
            items.CopyTo(_items.AsSpan(Count));
            Count = newCount;
        }

        public void AddRange(T[] items) => AddRange(items.AsSpan());

        public void AddRange(IEnumerable<T> items) {
            if (items is ICollection<T> collection) {
                var newCount = Count + collection.Count;
                if (newCount > _items.Length) EnsureCapacity(newCount);
                collection.CopyTo(_items, Count);
                Count = newCount;
            } else {
                foreach (var item in items) Add(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveRange(int index, int count) {
            if (count == 0) return;

            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            if ((uint)count > (uint)(Count - index)) throw new ArgumentOutOfRangeException(nameof(count));

            var items = _items;
            int newCount = Count - count;

            // 需要搬移尾部数据
            if (index < newCount) {
                Array.Copy(items, index + count, items, index, newCount - index);
            }

            // 清理尾部（仅引用类型需要）
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
                Array.Clear(items, newCount, count);
            }

            Count = newCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => _items.AsSpan(0, Count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> AsMemory() => _items.AsMemory(0, Count);

        public void EnsureCapacity(int capacity) {
            if (capacity <= Capacity) return;
            var newBuffer = new T[Math.Max(Math.Max(Count * 2, capacity), MinimumCapacity)];
            Array.Copy(_items, 0, newBuffer, 0, Count);
            _items = newBuffer;
        }

        public T[] GetRawBuffer() => _items;

        public void SetOffsetManually(int offset) {
            if ((uint)offset > (uint)_items.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            Count = offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new(_items, Count);

        public void TrimExcess() {
            var desired = Math.Max(Count, MinimumCapacity);
            if (desired == Capacity) return;
            var newBuffer = new T[desired];
            Array.Copy(_items, 0, newBuffer, 0, Count);
            _items = newBuffer;
        }

        public struct Enumerator : IEnumerator<T> {
            private readonly T[] _source;
            private readonly int _itemsCount;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(T[] source, int itemsCount) {
                _source = source;
                _itemsCount = itemsCount;
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref T GetCurrentRef() => ref _source[_index];

            public T Current => _source[_index];

            object? IEnumerator.Current => _source[_index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => unchecked(++_index < _itemsCount);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() => _index = -1;

            public void Dispose() {
            }
        }
    }
}
