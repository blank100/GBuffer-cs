namespace Gal.Core {
    /// <summary>
    /// 栈上的reader
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <author>gouanlin</author>
    public ref struct RefReader<T> {
        private readonly ReadOnlySpan<T> _span;
        private int _position;

        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span.Length;
        }

        public int Position {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                Debug.Assert(0 <= value && value <= _span.Length, $"{nameof(Position)} cannot be less than 0 or greater than {nameof(Length)}");
                _position = value;
            }
        }

        public int ReadableCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span.Length - _position;
        }

        public ReadOnlySpan<T> Span {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span[_position..];
        }

        public RefReader(ReadOnlySpan<T> original) {
            _span = original;
            _position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read() => _span[_position++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int count) {
            Debug.Assert(count >= 0, $"The parameter of {nameof(Advance)} cannot be negative");

            _position += count;
        }

        public ReadOnlySpan<T> GetSpan(int count) {
            Debug.Assert(count <= _span.Length - _position, $"The parameter {nameof(count)} cannot be negative");

            return _span.Slice(_position, count);
        }

        public void Dispose() {
            this = default;
        }
    }
}
