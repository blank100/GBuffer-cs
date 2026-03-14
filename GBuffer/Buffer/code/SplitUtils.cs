namespace Gal.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>gouanlin</author>
    public static class SplitUtils
    {
        public delegate void HandleElement<T>(ReadOnlySpan<T> slice);

        public delegate void HandleElement<T, in TUserData>(ReadOnlySpan<T> slice, TUserData userData);

        public delegate void HandleMultiElement<T>(int d, ReadOnlySpan<T> slice);

        public delegate void HandleMultiElement<T, in TUserData>(int d, ReadOnlySpan<T> slice, TUserData userData);

        public static void Split1D<T>(ReadOnlySpan<T> span, HandleElement<T> handler, T separator) where T : IEquatable<T> {
            int i;
            while ((i = span.IndexOf(separator)) != -1) {
                handler(span[..i]);
                span = span[++i..];
            }
            handler(span);
        }

        public static void Split1D<T, TUserData>(ReadOnlySpan<T> span, HandleElement<T, TUserData> handler, T separator, TUserData userData) where T : IEquatable<T> {
            int i;
            while ((i = span.IndexOf(separator)) != -1) {
                handler(span[..i], userData);
                span = span[++i..];
            }
            handler(span, userData);
        }

        public static void SplitMultiD<T>(ReadOnlySpan<T> span, HandleMultiElement<T> handler, ReadOnlySpan<T> separators) where T : IEquatable<T> {
            while (true) {
                int index = separators.Length - 1, i;
                var separator = separators[index];
                if (index > 0) {
                    while ((i = span.IndexOf(separator)) != -1) {
                        handler(index, ReadOnlySpan<T>.Empty);
                        SplitMultiD(span[..i], handler, separators[1..]);
                        span = span[++i..];
                    }
                    handler(index, ReadOnlySpan<T>.Empty);
                    if (span.Length > 0) {
                        separators = separators[1..];
                        continue;
                    }
                } else {
                    while ((i = span.IndexOf(separator)) != -1) {
                        handler(0, span[..i]);
                        span = span[++i..];
                    }
                    handler(0, span);
                }
                break;
            }
        }

        public static void SplitMultiD<T, TUserData>(ReadOnlySpan<T> span, HandleMultiElement<T, TUserData> handler, ReadOnlySpan<T> separators, TUserData userData) where T : IEquatable<T> {
            while (true) {
                int index = separators.Length - 1, i;
                var separator = separators[index];
                if (index > 0) {
                    while ((i = span.IndexOf(separator)) != -1) {
                        handler(index, ReadOnlySpan<T>.Empty, userData);
                        SplitMultiD(span[..i], handler, separators[1..], userData);
                        span = span[++i..];
                    }
                    handler(index, ReadOnlySpan<T>.Empty, userData);
                    if (span.Length > 0) {
                        separators = separators[1..];
                        continue;
                    }
                } else {
                    while ((i = span.IndexOf(separator)) != -1) {
                        handler(0, span[..i], userData);
                        span = span[++i..];
                    }
                    handler(0, span, userData);
                }
                break;
            }
        }
    }
}
