namespace Gal.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <author>gouanlin</author>
    public interface IReader<T> : IDisposable
    {
        int Length { get; }
        int Position { get; set; }

        /// <summary>
        /// 剩余可读取长度
        /// </summary>
        int ReadableCount { get; }

        ReadOnlyMemory<T> Memory { get; }
        ReadOnlySpan<T> Span { get; }

        T this[int index] { get; }

        T Read();

        ReadOnlySpan<T> GetSpan(int count);
        ReadOnlyMemory<T> GetMemory(int count);

        /// <summary>
        /// 指针向后移动指针
        /// </summary>
        /// <param name="count"></param>
        void Advance(int count);
    }
}
