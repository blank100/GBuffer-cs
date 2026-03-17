using Gal.Core;
using Xunit;

public class SpanByteWriterChainTests
{
    [Fact]
    public void ChainWrite_ShouldAdvanceSpanCorrectly()
    {
        Span<byte> buffer = stackalloc byte[16];
        var span = buffer;

        span.WriteUInt8(1);
        span.WriteUInt8(2);
        span.WriteUInt8(3);

        Assert.Equal(new byte[] {1,2,3}, buffer[..3].ToArray());
        Assert.Equal(13, span.Length);
    }

    [Fact]
    public void MixedWrites_ShouldBeSequential()
    {
        Span<byte> buffer = stackalloc byte[16];
        var span = buffer;

        span.WriteUInt8(1);
        span.WriteInt16(0x2233);
        span.WriteUInt8(4);

#if BIGENDIAN
        Assert.Equal(new byte[] {1,0x22,0x33,4}, buffer[..4].ToArray());
#else
        Assert.Equal(new byte[] {1,0x33,0x22,4}, buffer[..4].ToArray());
#endif
    }
}
