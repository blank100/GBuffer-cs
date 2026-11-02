using System.IO;

using Gal.Core;

using Xunit;

namespace Serialize.Test {
	/// <summary>
	/// 读取侧防御性校验：数据不足/非法时必须抛异常，而不是越界读取。
	/// </summary>
	public class GuardTest {
		[Fact]
		public void FixedWidthReadsThrowWhenSpanTooShort() {
			Assert.Throws<EndOfStreamException>(() => SpanByteUtils.ReadInt16(new byte[1]));
			Assert.Throws<EndOfStreamException>(() => SpanByteUtils.ReadUInt16(new byte[0]));
			Assert.Throws<EndOfStreamException>(() => SpanByteUtils.ReadInt32(new byte[3]));
			Assert.Throws<EndOfStreamException>(() => SpanByteUtils.ReadUInt32(new byte[2]));
			Assert.Throws<EndOfStreamException>(() => SpanByteUtils.ReadInt64(new byte[7]));
			Assert.Throws<EndOfStreamException>(() => SpanByteUtils.ReadUInt64(new byte[4]));
			Assert.Throws<EndOfStreamException>(() => SpanByteUtils.ReadFloat(new byte[3]));
			Assert.Throws<EndOfStreamException>(() => SpanByteUtils.ReadDouble(new byte[8 - 1]));
		}

		[Fact]
		public void ReaderVariantsThrowWhenNotEnoughData() {
			Assert.Throws<EndOfStreamException>(() => new Reader<byte>(new byte[] { 1, 2 }).ReadInt32());
			Assert.Throws<EndOfStreamException>(() => new Reader<byte>(new byte[] { 1 }).ReadInt16());

			Assert.Throws<EndOfStreamException>(() => RefReadInt32FromTwoBytes());
		}

		[Fact]
		public void ReadUtf8RejectsTruncatedPayload() {
			// 长度前缀声称 5 字节，实际只有 1 字节
			var bytes = new byte[] { 0x05, 0x00, (byte) 'a' };
			Assert.Throws<FormatException>(() => SpanByteUtils.ReadUtf8(bytes, out _));
		}

		[Fact]
		public void ReadUtf8WithExplicitLengthRejectsOverflow() {
			Assert.Throws<FormatException>(() => SpanByteUtils.ReadUtf8(new byte[] { 1, 2 }, 5));
			Assert.Equal("ab", SpanByteUtils.ReadUtf8(new byte[] { (byte) 'a', (byte) 'b' }, 2));
		}

		[Fact]
		public void VarIntSlowPathWorksOnShortSpans() {
			// span.Length < 8 走 Slow 路径
			var bytes = new byte[] { 0x80, 0x01 };
			Assert.Equal(128u, SpanByteUtils.ReadVarUInt32(bytes, out var count));
			Assert.Equal(2, count);

			Assert.Throws<FormatException>(() => SpanByteUtils.ReadVarUInt32(new byte[] { 0x80 }, out _));
		}

		[Fact]
		public void IsAsciiDetectsNonAsciiBytes() {
			Assert.True(SpanByteUtils.IsAscii("hello 123 !@#"u8));
			Assert.True(SpanByteUtils.IsAscii(ReadOnlySpan<byte>.Empty));
			Assert.False(SpanByteUtils.IsAscii("中文"u8));
			Assert.False(SpanByteUtils.IsAscii(new byte[] { 0x61, 0x80, 0x62 }));
		}

		[Fact]
		public void WriteUtf8RejectsOverlongString() {
			using var w = new Writer<byte>(8);
			Assert.Throws<ArgumentOutOfRangeException>(() => w.WriteUtf8(new string('a', 40000)));
		}
		// ref struct 不能出现在 lambda 中，用普通方法绕过
		private static int RefReadInt32FromTwoBytes() {
			var r = new RefReader<byte>(new byte[] { 1, 2 });
			return r.ReadInt32();
		}
	}
}
