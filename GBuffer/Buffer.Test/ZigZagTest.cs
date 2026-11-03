using Gal.Core;

using Xunit;

namespace Serialize.Test {
	public class ZigZagTest {
		[Fact]
		public void EncodeZigZag32BoundaryValues() {
			Assert.Equal(0x00000000u, ZigZagUtils.EncodeZigZag32(0));
			Assert.Equal(0x00000001u, ZigZagUtils.EncodeZigZag32(-1));
			Assert.Equal(0x00000002u, ZigZagUtils.EncodeZigZag32(1));
			Assert.Equal(0x00000003u, ZigZagUtils.EncodeZigZag32(-2));
			Assert.Equal(0xFFFFFFFEu, ZigZagUtils.EncodeZigZag32(int.MaxValue));
			Assert.Equal(0xFFFFFFFFu, ZigZagUtils.EncodeZigZag32(int.MinValue));
		}

		[Fact]
		public void EncodeZigZag64BoundaryValues() {
			Assert.Equal(0x0000000000000000ul, ZigZagUtils.EncodeZigZag64(0L));
			Assert.Equal(0x0000000000000001ul, ZigZagUtils.EncodeZigZag64(-1L));
			Assert.Equal(0x0000000000000002ul, ZigZagUtils.EncodeZigZag64(1L));
			Assert.Equal(0xFFFFFFFFFFFFFFFEul, ZigZagUtils.EncodeZigZag64(long.MaxValue));
			Assert.Equal(0xFFFFFFFFFFFFFFFFul, ZigZagUtils.EncodeZigZag64(long.MinValue));
		}

		[Fact]
		public void ZigZagRoundTripsRandomValues() {
			var rng = Random.Shared;
			for (var i = 0; i < 100000; i++) {
				var v32 = rng.Next(int.MinValue, int.MaxValue);
				Assert.Equal(v32, ZigZagUtils.DecodeZigZag32(ZigZagUtils.EncodeZigZag32(v32)));

				var v64 = ((long) rng.Next(int.MinValue, int.MaxValue) << 32) | (uint) rng.Next(int.MinValue, int.MaxValue);
				Assert.Equal(v64, ZigZagUtils.DecodeZigZag64(ZigZagUtils.EncodeZigZag64(v64)));
			}
		}
	}
}
