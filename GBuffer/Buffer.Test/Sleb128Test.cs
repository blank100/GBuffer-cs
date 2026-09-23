using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class Sleb128Test {
		[Fact]
		public void Int32RoundTripsBoundaryValues() {
			var values = new[] { int.MinValue, -8193, -8192, -129, -128, -1, 0, 1, 63, 64, 127, 128, int.MaxValue };
			Span<byte> bytes = stackalloc byte[5];

			foreach (var value in values) {
				var written = SpanByteUtils.WriteSleb128Int32(bytes, value);
				var result = SpanByteUtils.ReadSleb128Int32(bytes, out var read);

				Assert.Equal(value, result);
				Assert.Equal(written, read);
			}
		}

		[Fact]
		public void Int64RoundTripsBoundaryValues() {
			var values = new[] { long.MinValue, -8193, -8192, -129, -128, -1, 0, 1, 63, 64, 127, 128, long.MaxValue };
			Span<byte> bytes = stackalloc byte[10];

			foreach (var value in values) {
				var written = SpanByteUtils.WriteSleb128Int64(bytes, value);
				var result = SpanByteUtils.ReadSleb128Int64(bytes, out var read);

				Assert.Equal(value, result);
				Assert.Equal(written, read);
			}
		}

		[Fact]
		public void ReaderWriterVariantsRoundTrip() {
			const int intValue = -123456;
			const long longValue = -1234567890123L;

			using var buffer = new Buffer<byte>(32);
			buffer.WriteSleb128Int32(intValue);
			buffer.WriteSleb128Int64(longValue);
			buffer.Position = 0;

			Assert.Equal(intValue, buffer.ReadSleb128Int32());
			Assert.Equal(longValue, buffer.ReadSleb128Int64());

			Span<byte> bytes = stackalloc byte[32];
			var writer = new RefWriter<byte>(bytes);
			writer.WriteSleb128Int32(intValue);
			writer.WriteSleb128Int64(longValue);

			var refReader = new RefReader<byte>(bytes);
			Assert.Equal(intValue, refReader.ReadSleb128Int32());
			Assert.Equal(longValue, refReader.ReadSleb128Int64());

			var reader = new Reader<byte>(bytes.ToArray());
			Assert.Equal(intValue, reader.ReadSleb128Int32());
			Assert.Equal(longValue, reader.ReadSleb128Int64());
		}
	}
}
