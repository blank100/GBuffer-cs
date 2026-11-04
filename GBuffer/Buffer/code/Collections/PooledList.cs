#nullable enable
using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Gal.Core
{
	[Serializable]
	public sealed class PooledList<T> : IList<T>, IList, IReadOnlyList<T>, IDisposable
	{
		private const int MinimumCapacity = 32;

		private T[] _items;
		[NonSerialized] private object? _syncRoot;
		private readonly ArrayPool<T> _arrayPool = ArrayPool<T>.Shared;
		private bool _disposed;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PooledList() {
			_items = _arrayPool.Rent(MinimumCapacity);
			Count = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PooledList(int capacity) {
			var rentSize = capacity < MinimumCapacity ? MinimumCapacity : capacity;
			_items = _arrayPool.Rent(rentSize);
			Count = 0;
		}

		public PooledList(IEnumerable<T> source) {
			if (source is null) throw new ArgumentNullException(nameof(source));

			switch (source) {
				case ICollection<T> collection: {
					var rentSize = collection.Count > MinimumCapacity ? collection.Count : MinimumCapacity;
					var buffer = _arrayPool.Rent(rentSize);

					collection.CopyTo(buffer, 0);

					_items = buffer;
					Count = collection.Count;
					break;
				}
				default: {
					_items = _arrayPool.Rent(MinimumCapacity);
					var buffer = _items;
					Count = 0;
					var count = 0;
					using var enumerator = source.GetEnumerator();
					while (enumerator.MoveNext()) {
						if (count < buffer.Length) {
							buffer[count] = enumerator.Current!;
							count++;
						} else {
							Count = count;
							AddWithResize(enumerator.Current!);
							count++;
							buffer = _items;
						}
					}

					Count = count;
					break;
				}
			}
		}

		public PooledList(T[] source) {
			if (source is null) throw new ArgumentNullException(nameof(source));

			var rentSize = source.Length > MinimumCapacity ? source.Length : MinimumCapacity;
			var buffer = _arrayPool.Rent(rentSize);
			source.CopyTo(buffer, 0);

			_items = buffer;
			Count = source.Length;
		}

		public PooledList(ReadOnlySpan<T> source) {
			var rentSize = source.Length > MinimumCapacity ? source.Length : MinimumCapacity;
			var buffer = _arrayPool.Rent(rentSize);
			source.CopyTo(buffer);

			_items = buffer;
			Count = source.Length;
		}

		public int Capacity => _items.Length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose() {
			if (_disposed) return;
			_disposed = true;

			_arrayPool.Return(_items, clearArray:RuntimeHelpers.IsReferenceOrContainsReferences<T>());
			_items = Array.Empty<T>();
			Count = 0;
		}

		private void ThrowIfDisposed() {
			if (_disposed) throw new ObjectDisposedException(nameof(PooledList<T>));
		}

		int ICollection.Count => Count;
		bool IList.IsFixedSize => false;
		bool ICollection.IsSynchronized => false;
		bool IList.IsReadOnly => false;

		object ICollection.SyncRoot {
			get {
				if (_syncRoot is null) {
					Interlocked.CompareExchange<object?>(ref _syncRoot, new object(), null);
				}

				return _syncRoot!;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int IList.Add(object? item) {
			ThrowIfDisposed();
			if (item is T itemAsTSource) {
				Add(itemAsTSource);
				return Count - 1;
			}

			throw new ArgumentException($"Wrong value type. Expected {typeof(T)}, got: '{item}'.", nameof(item));
		}

		bool IList.Contains(object? item) {
			if (item is T itemAsTSource) return Contains(itemAsTSource);
			return false;
		}

		int IList.IndexOf(object? item) {
			if (item is T itemAsTSource) return IndexOf(itemAsTSource);
			return -1;
		}

		void IList.Remove(object? item) {
			if (item is T itemAsTSource) Remove(itemAsTSource);
		}

		void IList.Insert(int index, object? item) {
			if (item is T itemAsTSource) {
				Insert(index, itemAsTSource);
				return;
			}

			throw new ArgumentException($"Wrong value type. Expected {typeof(T)}, got: '{item}'.", nameof(item));
		}

		void ICollection.CopyTo(Array array, int arrayIndex) {
			ThrowIfDisposed();
			if (array is null) throw new ArgumentNullException(nameof(array));
			if (array.Rank != 1) throw new ArgumentException("Array must be single dimensional", nameof(array));
			if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			if (array.Length - arrayIndex < Count) throw new ArgumentException("Destination array is not long enough.");

			if (array is T[] tArray) {
				Array.Copy(_items, 0, tArray, arrayIndex, Count);
				return;
			}

			Array.Copy(_items, 0, array, arrayIndex, Count);
		}

#if NETSTANDARD2_1
		[MaybeNull]
#endif
		object IList.this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				ThrowIfDisposed();
				if ((uint)index >= (uint)Count) throw new IndexOutOfRangeException(nameof(index));
				return _items[index]!;
			}

			set {
				ThrowIfDisposed();
				if ((uint)index >= (uint)Count) throw new IndexOutOfRangeException(nameof(index));
				if (value is T valueAsTSource) {
					_items[index] = valueAsTSource;
				} else {
					throw new ArgumentException($"Wrong value type. Expected {typeof(T)}, got: '{value}'.", nameof(value));
				}
			}
		}

		public int Count { get; private set; }

		public bool IsReadOnly => false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(T item) {
			ThrowIfDisposed();
			var buffer = _items;
			var count = Count;
			if (count < buffer.Length) {
				buffer[count] = item;
				Count = count + 1;
			} else {
				AddWithResize(item);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear() {
			ThrowIfDisposed();
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) Array.Clear(_items, 0, Count);
			Count = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(T item) => IndexOf(item) > -1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int IndexOf(T item) => Array.IndexOf(_items, item, 0, Count);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CopyTo(T[] array, int arrayIndex) => Array.Copy(_items, 0, array, arrayIndex, Count);

		public bool Remove(T item) {
			ThrowIfDisposed();
			var index = IndexOf(item);
			if (index == -1) return false;
			RemoveAt(index);
			return true;
		}

		public void Insert(int index, T item) {
			ThrowIfDisposed();
			var count = Count;
			var buffer = _items;

			if (buffer.Length == count) {
				var newCapacity = count == 0 ? MinimumCapacity : count * 2;
				EnsureCapacity(newCapacity);
				buffer = _items;
			}

			if (index < 0 || index > count) throw new ArgumentOutOfRangeException(nameof(index));

			if (index < count) Array.Copy(buffer, index, buffer, index + 1, count - index);
			buffer[index] = item;
			Count++;
		}

		public void RemoveAt(int index) {
			ThrowIfDisposed();
			var count = Count;
			var buffer = _items;

			if ((uint)index >= (uint)count) throw new ArgumentOutOfRangeException(nameof(index));

			count--;
			if (index < count) Array.Copy(buffer, index + 1, buffer, index, count - index);
			if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) buffer[count] = default!;
			Count = count;
		}

#if NETSTANDARD2_1
		[MaybeNull]
#endif
		public T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				ThrowIfDisposed();
				if ((uint)index >= (uint)Count) throw new IndexOutOfRangeException(nameof(index));
				return _items[index];
			}

			set {
				ThrowIfDisposed();
				if ((uint)index >= (uint)Count) throw new IndexOutOfRangeException(nameof(index));
				_items[index] = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(_items, Count);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_items, Count);

		public void AddRange(ReadOnlySpan<T> items) {
			ThrowIfDisposed();
			var count = Count;
			var buffer = _items;

			if (count + items.Length > buffer.Length) {
				EnsureCapacity(count + items.Length);
				buffer = _items;
			}

			items.CopyTo(buffer.AsSpan(count));
			Count += items.Length;
		}

		public void AddRange(T[] items) {
			ThrowIfDisposed();
			var count = Count;
			var buffer = _items;

			if (count + items.Length > buffer.Length) {
				EnsureCapacity(count + items.Length);
				buffer = _items;
			}

			Array.Copy(items, 0, buffer, count, items.Length);
			Count += items.Length;
		}

		public void AddRange(IEnumerable<T> items) {
			ThrowIfDisposed();
			var count = Count;
			var buffer = _items;

			if (items is ICollection<T> collection) {
				if (count + collection.Count > buffer.Length) {
					EnsureCapacity(count + collection.Count);
					buffer = _items;
				}

				collection.CopyTo(buffer, count);
				Count += collection.Count;
			} else {
				foreach (var item in items) {
					if (count < buffer.Length) {
						buffer[count] = item;
						count++;
					} else {
						Count = count;// flush
						AddWithResize(item);
						count++;
						buffer = _items;
					}
				}

				Count = count;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<T> AsSpan() {
			ThrowIfDisposed();
			return _items.AsSpan(0, Count);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Memory<T> AsMemory() {
			ThrowIfDisposed();
			return _items.AsMemory(0, Count);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void AddWithResize(T item) {
			var oldBuffer = _items;
			var oldCount = Count;
			var requested = oldBuffer.Length * 2;
			if (requested < MinimumCapacity) requested = MinimumCapacity;
			var newBuffer = _arrayPool.Rent(requested);

			Array.Copy(oldBuffer, 0, newBuffer, 0, oldCount);

			newBuffer[oldCount] = item;
			_items = newBuffer;
			Count = oldCount + 1;
			_arrayPool.Return(oldBuffer, clearArray:RuntimeHelpers.IsReferenceOrContainsReferences<T>());
		}

		public void EnsureCapacity(int capacity) {
			ThrowIfDisposed();
			if (capacity <= this.Capacity) return;
			var desired = Math.Max(capacity, MinimumCapacity);
			var newBuffer = _arrayPool.Rent(desired);
			Array.Copy(_items, 0, newBuffer, 0, Count);
			_arrayPool.Return(_items, clearArray:RuntimeHelpers.IsReferenceOrContainsReferences<T>());
			_items = newBuffer;
		}

		public T[] GetRawBuffer() => _items;

		public void SetOffsetManually(int offset) => Count = offset;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Enumerator GetEnumerator() => new(_items, Count);

		public void TrimExcess() {
			ThrowIfDisposed();
			var desired = Math.Max(Count, MinimumCapacity);
			if (desired >= Capacity) return;
			var newBuffer = _arrayPool.Rent(desired);
			Array.Copy(_items, 0, newBuffer, 0, Count);
			_arrayPool.Return(_items, clearArray:RuntimeHelpers.IsReferenceOrContainsReferences<T>());
			_items = newBuffer;
		}

		public struct Enumerator : IEnumerator<T>
		{
			private readonly T[] _source;
			private readonly int _itemsCount;
			private int _index;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Enumerator(T[] source, int itemsCount) {
				_source = source;
				_itemsCount = itemsCount;
				_index = -1;
			}

			/// <summary>
			/// High-performance by-ref accessor. Use carefully.
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public ref T GetCurrentRef() => ref _source[_index];

			public T Current => _source[_index];

			object? IEnumerator.Current => _source[_index];

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext() => unchecked(++_index < _itemsCount);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset() => _index = -1;

			public void Dispose() { }
		}
	}
}
