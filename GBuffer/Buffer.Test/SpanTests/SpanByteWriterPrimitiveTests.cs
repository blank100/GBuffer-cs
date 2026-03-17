using System;
using Gal.Core;
using Xunit;

namespace Gal.Core.Tests
{
    public class SpanByteWriterPrimitiveTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(255)]
        public void WriteUInt8(byte value)
        {
            Span<byte> buffer = stackalloc byte[8];
            var span = buffer;

            span.WriteUInt8(value);

            Assert.Equal(value, buffer[0]);
            Assert.Equal(7, span.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(127)]
        [InlineData(-128)]
        public void WriteInt8(sbyte value)
        {
            Span<byte> buffer = stackalloc byte[8];
            var span = buffer;

            span.WriteInt8(value);

            Assert.Equal((byte)value, buffer[0]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(short.MaxValue)]
        [InlineData(short.MinValue)]
        public void WriteInt16(int value)
        {
            Span<byte> buffer = stackalloc byte[8];
            var span = buffer;

            span.WriteInt16(value);

            var expected = BitConverter.GetBytes((short)value);

#if BIGENDIAN
            Array.Reverse(expected);
#endif

            Assert.Equal(expected, buffer[..2].ToArray());
        }
    }
}
