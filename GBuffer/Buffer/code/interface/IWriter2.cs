using System.Buffers;

namespace Gal.Core {
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TSelf"></typeparam>
    /// <typeparam name="TData"></typeparam>
    /// <author>gouanlin</author>
    public interface IWriter2<out TSelf, TData> : IDisposable where TSelf: IWriter2<TSelf, TData> {
        int Capacity { get; }
        int Length { get; set; }
        int Position { get; set; }
        int WritableCount { get; }

        /// <summary>
        /// 获取已写入的内容的 Span&lt;T&gt;
        /// </summary>
        Span<TData> WrittenSpan { get; }

        /// <summary>
        /// 获取已写入的内容的 Memory&lt;T&gt;
        /// </summary>
        Memory<TData> WrittenMemory { get; }

        /// <summary>
        /// 获取当前位置到结尾的 Span&lt;T&gt;
        /// <remarks>此属性应慎用, 在持有返回的 Span&lt;T&gt; 对象期间,如果发生了扩容等修改原始数组的操作,应重新获取 Span&lt;T&gt;</remarks>
        /// </summary>
        Span<TData> Span { get; }

        /// <summary>
        /// 获取当前位置到结尾的 Memory&lt;T&gt;
        /// <remarks>此属性应慎用, 在持有返回的 Memory&lt;T&gt; 对象期间,如果发生了扩容等修改原始数组的操作,应重新获取 Memory&lt;T&gt;</remarks>
        /// </summary>
        Memory<TData> Memory { get; }

        /// <summary>
        /// 获取原始数组
        /// <remarks>此属性应慎用, 在持有返回的 数组 对象期间,禁止执行会导致原始数组扩容等效果的操作</remarks>
        /// </summary>
        TData[] RawArray { get; }

        TData this[int index] { set; }

        TSelf Write(TData element);
        TSelf Write(TData element1, TData element2);
        TSelf Write(TData element1, TData element2, TData element3);

        TSelf Write(ReadOnlySpan<TData> elements);
        TSelf Write(ReadOnlyMemory<TData> elements);
        TSelf Write(ReadOnlySequence<TData> elements);

        /// <summary>
        /// 获取当前位置到 buffer 结束的 Span&lt;T&gt;,如果长度不足 sizeHint ,则扩充 buffer 长度
        /// </summary>
        /// <param name="sizeHint"></param>
        /// <returns></returns>
        Span<TData> GetSpan(int sizeHint = 0);

        /// <summary>
        /// 获取当前位置到 buffer 结束的 Memory&lt;T&gt;,如果长度不足 sizeHint ,则扩充 buffer 长度
        /// </summary>
        /// <param name="sizeHint"></param>
        /// <returns></returns>
        Memory<TData> GetMemory(int sizeHint = 0);

        /// <summary>
        /// 提示需要指定长度的空间
        /// <para>即从当前位置到 capacity 需要指定长度的空间,不足则扩充 buffer 到足够长度</para>
        /// </summary>
        /// <param name="sizeHint"></param>
        void HintSize(int sizeHint);

        /// <summary>
        /// 指针向后移动指针
        /// </summary>
        /// <param name="count"></param>
        void Advance(int count);

        TSelf Clear();

        /// <summary>
        /// 丢弃当前位置之前的所有数据
        /// </summary>
        /// <returns></returns>
        TSelf Discard();
    }
}
