namespace Gal.Core
{
	/// <summary>
	/// 
	/// </summary>
	/// <author>gouanlin</author>
	public static class ReadonlySpanExtension
	{
		/// <summary>
		/// 读取到指定元素
		/// </summary>
		/// <param name="span"></param>
		/// <param name="separator"></param>
		/// <param name="v"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns>指定元素存在,则返回 true </returns>
		public static bool ReadTo<T>(this ref ReadOnlySpan<T> span, T separator, out ReadOnlySpan<T> v) where T : IEquatable<T> {
			var i = span.IndexOf(separator);
			if (i == -1) {
				v = span;
				return false;
			}
			v = span[..i];
			span = span[++i..];
			return true;
		}
	}
}
