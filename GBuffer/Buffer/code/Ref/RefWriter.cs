using System.Buffers;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Gal.Core
{
	/// <summary>
	/// 栈上的 Writer
	/// </summary>
	/// <author>gouanlin</author>
	/// <typeparam name="T"></typeparam>
	public ref struct RefWriter<T>
	{
		private T[] _buffer;
		private Span<T> _span;
		private int _position;
		private int _length;

		/// <summary>
		/// 长度
		/// </summary>
		public int Length {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _length;
			set {
				Debug.Assert(value >= 0, $"{nameof(Length)} cannot be less than 0");

				_length = value;
				if (value > _span.Length) GrowBuffer(value - _span.Length);
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
				Debug.Assert(0 <= value && value <= _span.Length, $"{nameof(Position)} cannot be less than 0 or greater than {nameof(Length)}");

				_position = value;
			}
		}

		/// <summary>
		/// 容量
		/// </summary>
		public int Capacity {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _span.Length;
		}

		/// <summary>
		/// 长度减去当前位置
		/// </summary>
		public int Remaining {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _length - _position;
		}

		/// <summary>
		/// 获取已写入数据的 memory
		/// <para>即从 0 到 length 的 memory</para>
		/// </summary>
		public Memory<T> WrittenMemory {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (_buffer == null) GenerateBuffer(_span.Length);
				return _buffer.AsMemory(0, _length);
			}
		}

		/// <summary>
		/// 获取 memory
		/// <para>即当前位置到 capacity 的 memory</para>
		/// </summary>
		public Memory<T> Memory {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (_buffer == null) GenerateBuffer(_span.Length);
				return _buffer.AsMemory(_position);
			}
		}

		/// <summary>
		/// 获取已写入数据的 span
		/// <para>即从 0 到 length 的 span</para>
		/// </summary>
		public Span<T> WrittenSpan {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _span[.._length];
		}

		/// <summary>
		/// 获取 span
		/// <para>即当前位置到 capacity 的 span</para>
		/// </summary>
		public Span<T> Span {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _span[_position..];
		}

		public RefWriter(Span<T> buffer) {
			_buffer = default;
			_span = buffer;
			_position = 0;
			_length = 0;
		}

		public RefWriter(int capacity) {
			Debug.Assert(capacity >= 0, $"The parameter {nameof(capacity)} cannot be negative");

			_buffer = ArrayPool<T>.Shared.Rent(capacity);
			_span = _buffer;
			_position = 0;
			_length = 0;
		}

		public T this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _span[index];
		}

		/// <summary>
		/// 在当前位置写入一个元素,并将 position 向后移动1位
		/// </summary>
		/// <param name="element"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(T element) {
			var t = _span;
			var p = _position;
			var n = p + 1;
			if (n > t.Length) {
				GrowBuffer(1);
				t = _span;
			}

			t[p] = element;

			_position = n;
			if (_length < n) _length = n;
		}

		/// <summary>
		/// 在当前位置写入两个元素,并将 position 向后移动2位
		/// </summary>
		/// <param name="element1"></param>
		/// <param name="element2"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(T element1, T element2) {
			var p = _position;
			HintSize(2);

			ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference(_span), p);
			Unsafe.Add(ref b, 0) = element1;
			Unsafe.Add(ref b, 1) = element2;

			var n = p + 2;
			_position = n;
			if (n > _length) _length = n;
		}

		/// <summary>
		/// 在当前位置写入三个元素,并将 position 向后移动3位
		/// </summary>
		/// <param name="element1"></param>
		/// <param name="element2"></param>
		/// <param name="element3"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(T element1, T element2, T element3) {
			var p = _position;
			HintSize(3);

			ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference(_span), p);
			Unsafe.Add(ref b, 0) = element1;
			Unsafe.Add(ref b, 1) = element2;
			Unsafe.Add(ref b, 2) = element3;

			var n = p + 3;
			_position = n;
			if (n > _length) _length = n;
		}

		/// <summary>
		/// 在当前位置写入三个元素,并将 position 向后移动3位
		/// </summary>
		/// <param name="element1"></param>
		/// <param name="element2"></param>
		/// <param name="element3"></param>
		/// <param name="element4"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(T element1, T element2, T element3, T element4) {
			var p = _position;
			HintSize(4);

			ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference(_span), p);
			Unsafe.Add(ref b, 0) = element1;
			Unsafe.Add(ref b, 1) = element2;
			Unsafe.Add(ref b, 2) = element3;
			Unsafe.Add(ref b, 3) = element4;

			var n = p + 4;
			_position = n;
			if (n > _length) _length = n;
		}

		/// <summary>
		/// 在当前位置写入三个元素,并将 position 向后移动3位
		/// </summary>
		/// <param name="element1"></param>
		/// <param name="element2"></param>
		/// <param name="element3"></param>
		/// <param name="element4"></param>
		/// <param name="element5"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(T element1, T element2, T element3, T element4, T element5) {
			var p = _position;
			HintSize(5);

			ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference(_span), p);
			Unsafe.Add(ref b, 0) = element1;
			Unsafe.Add(ref b, 1) = element2;
			Unsafe.Add(ref b, 2) = element3;
			Unsafe.Add(ref b, 3) = element4;
			Unsafe.Add(ref b, 4) = element5;

			var n = p + 5;
			_position = n;
			if (n > _length) _length = n;
		}

		/// <summary>
		/// 在当前位置写入三个元素,并将 position 向后移动3位
		/// </summary>
		/// <param name="element1"></param>
		/// <param name="element2"></param>
		/// <param name="element3"></param>
		/// <param name="element4"></param>
		/// <param name="element5"></param>
		/// <param name="element6"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(T element1, T element2, T element3, T element4, T element5, T element6) {
			var p = _position;
			HintSize(6);

			ref var b = ref Unsafe.Add(ref MemoryMarshal.GetReference(_span), p);
			Unsafe.Add(ref b, 0) = element1;
			Unsafe.Add(ref b, 1) = element2;
			Unsafe.Add(ref b, 2) = element3;
			Unsafe.Add(ref b, 3) = element4;
			Unsafe.Add(ref b, 4) = element5;
			Unsafe.Add(ref b, 5) = element6;

			var n = p + 6;
			_position = n;
			if (n > _length) _length = n;
		}

		/// <summary>
		/// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
		/// </summary>
		/// <param name="elements"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(ReadOnlySpan<T> elements) {
			var count = elements.Length;
			if(count == 0) return;
			HintSize(count);
			elements.CopyTo(_span[_position..]);
			Advance(count);
		}

		/// <summary>
		/// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
		/// </summary>
		/// <param name="elements"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(ReadOnlyMemory<T> elements) {
			var count = elements.Length;
			if(count == 0) return;
			HintSize(count);
			elements.Span.CopyTo(_span[_position..]);
			Advance(count);
		}

		/// <summary>
		/// 在当前位置写入一个元素序列,并将 position 向后移动到新的位置
		/// </summary>
		/// <param name="elements"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(ReadOnlySequence<T> elements) {
			var count = (int)elements.Length;
			if(count == 0) return;
			HintSize(count);
			elements.CopyTo(_span[_position..]);
			Advance(count);
		}

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
			_span[..Math.Min(_length, _span.Length)].CopyTo(buffer);
			if (_buffer != null) ArrayPool<T>.Shared.Return(_buffer, !typeof(T).IsValueType);
			_span = _buffer = buffer;
		}

		/// <summary>
		/// 指针向后移动
		/// </summary>
		/// <param name="count"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Advance(int count) {
			Debug.Assert(_position + count >= 0, $"移动后的指针位置不能未负数");
			Debug.Assert(_position + count <= _span.Length, "移动后的指针位置超出了buffer的容量");


			var t = _position += count;
			if (_length < t) _length = t;
		}

		/// <summary>
		/// 清理
		/// <para>不会真实的清理所有元素,只是将 position 和 length 置为 0 </para>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear() => Length = 0;

		/// <summary>
		/// 获取 span
		/// </summary>
		/// <param name="sizeHint">需要的 span 的长度, 不足则会扩充 buffer 到足够长度, 此参数为 0 , 则返回当前位置到 capacity 的 span </param>
		/// <returns></returns>
		public Span<T> GetSpan(int sizeHint = 0) {
			Debug.Assert(sizeHint >= 0, $"The parameter of {nameof(sizeHint)} cannot be negative");
			if (sizeHint == 0) return _span[_position..];
			HintSize(sizeHint);
			return _span[_position..];
		}

		/// <summary>
		/// 获取 memory
		/// </summary>
		/// <param name="sizeHint">需要的 memory 的长度, 不足则会扩充 buffer 到足够长度, 此参数为 0 , 则返回当前位置到 capacity 的 memory </param>
		/// <returns></returns>
		public Memory<T> GetMemory(int sizeHint = 0) {
			Debug.Assert(sizeHint >= 0, $"The parameter of {nameof(sizeHint)} cannot be negative");

			if (sizeHint == 0) {
				if (_buffer == null) GenerateBuffer(_span.Length);
				return _buffer.AsMemory(_position);
			}
			var availableSize = _span.Length - _position;
			if (availableSize < sizeHint) GrowBuffer(sizeHint - availableSize);
			else if (_buffer == null) GenerateBuffer(_span.Length);
			return _buffer.AsMemory(_position);
		}

		/// <summary>
		/// 提示需要指定长度的空间
		/// <para>即从当前位置到 capacity 需要指定长度的空间,不足则扩充 buffer 到足够长度</para>
		/// </summary>
		/// <param name="sizeHint"></param>
		public void HintSize(int sizeHint) {
			Debug.Assert(sizeHint > 0, $"The parameter of {nameof(sizeHint)} must be greater than 0");

			var availableSize = _span.Length - _position;
			if (availableSize >= sizeHint) return;
			GrowBuffer(sizeHint - availableSize);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose() {
			var buffer = _buffer;
			this = default;
			if (buffer != null) ArrayPool<T>.Shared.Return(buffer, !typeof(T).IsValueType);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool InternalEnsureCapacity(ref int capacity, int need, ref int written, int remaining, ref Span<T> span) {
			if (capacity - written >= need) return false;
			Advance(written);
			written = 0;
			HintSize(Math.Max((int)(remaining * 1.1f), remaining + need));
			span = this.Span;
			capacity = span.Length;
			return true;
		}
	}
}
