using System;
using System.Text;
using Xunit;
using Gal.Core;

namespace Gal.Core.Tests
{
    public class SpanByteWriterExtensionTests
    {
        [Fact]
        public void WriteUInt8_ShouldWriteCorrectByte()
        {
            Span<byte> buffer = stackalloc byte[8];
            var span = buffer;

            span.WriteUInt8(123);

            Assert.Equal((byte)123, buffer[0]);
            Assert.Equal(7, span.Length);
        }

        [Fact]
        public void WriteInt8_ShouldWriteCorrectByte()
        {
            Span<byte> buffer = stackalloc byte[8];
            var span = buffer;

            span.WriteInt8(-1);

            Assert.Equal((byte)255, buffer[0]);
            Assert.Equal(7, span.Length);
        }

        [Fact]
        public void WriteInt16_ShouldWriteLittleEndian()
        {
            Span<byte> buffer = stackalloc byte[8];
            var span = buffer;

            span.WriteInt16(0x1234);

#if BIGENDIAN
            Assert.Equal(0x12, buffer[1]);
            Assert.Equal(0x34, buffer[0]);
#else
            Assert.Equal(0x34, buffer[0]);
            Assert.Equal(0x12, buffer[1]);
#endif
            Assert.Equal(6, span.Length);
        }

        [Fact]
        public void WriteInt32_ShouldWriteCorrectBytes()
        {
            Span<byte> buffer = stackalloc byte[8];
            var span = buffer;

            span.WriteInt32(0x11223344);

#if BIGENDIAN
            Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0x44 }, buffer[..4].ToArray());
#else
            Assert.Equal(new byte[] { 0x44, 0x33, 0x22, 0x11 }, buffer[..4].ToArray());
#endif

            Assert.Equal(4, span.Length);
        }

        [Fact]
        public void WriteUInt32_ShouldWriteCorrectBytes()
        {
            Span<byte> buffer = stackalloc byte[8];
            var span = buffer;

            span.WriteUInt32(0xAABBCCDD);

#if BIGENDIAN
            Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, buffer[..4].ToArray());
#else
            Assert.Equal(new byte[] { 0xDD, 0xCC, 0xBB, 0xAA }, buffer[..4].ToArray());
#endif

            Assert.Equal(4, span.Length);
        }

        [Fact]
        public void WriteInt64_ShouldWriteCorrectBytes()
        {
            Span<byte> buffer = stackalloc byte[16];
            var span = buffer;

            span.WriteInt64(0x1122334455667788);

#if BIGENDIAN
            Assert.Equal(
                new byte[] {0x11,0x22,0x33,0x44,0x55,0x66,0x77,0x88},
                buffer[..8].ToArray());
#else
            Assert.Equal(
                new byte[] {0x88,0x77,0x66,0x55,0x44,0x33,0x22,0x11},
                buffer[..8].ToArray());
#endif

            Assert.Equal(8, span.Length);
        }

        [Fact]
        public void WriteFloat_ShouldWriteCorrectBits()
        {
            Span<byte> buffer = stackalloc byte[8];
            var span = buffer;

            span.WriteFloat(1.5f);

            var expected = BitConverter.GetBytes(1.5f);

#if BIGENDIAN
            Array.Reverse(expected);
#endif

            Assert.Equal(expected, buffer[..4].ToArray());
        }

        [Fact]
        public void WriteDouble_ShouldWriteCorrectBits()
        {
            Span<byte> buffer = stackalloc byte[16];
            var span = buffer;

            span.WriteDouble(123.456);

            var expected = BitConverter.GetBytes(123.456);

#if BIGENDIAN
            Array.Reverse(expected);
#endif

            Assert.Equal(expected, buffer[..8].ToArray());
        }

        [Fact]
        public void WriteUtf8_ShouldWriteString()
        {
            Span<byte> buffer = stackalloc byte[64];
            var span = buffer;

            span.WriteUtf8("hello");

            var len = BitConverter.ToInt16(buffer[..2]);

#if BIGENDIAN
            len = (short)((buffer[0] << 8) | buffer[1]);
#endif

            Assert.Equal(5, len);

            var str = Encoding.UTF8.GetString(buffer.Slice(2, len));
            Assert.Equal("hello", str);
        }

        [Fact]
        public void WriteUtf8_ShouldHandleEmpty()
        {
            Span<byte> buffer = stackalloc byte[16];
            var span = buffer;

            span.WriteUtf8("");

            Assert.Equal(0, buffer[0]);
            Assert.Equal(0, buffer[1]);
        }
    }
}
