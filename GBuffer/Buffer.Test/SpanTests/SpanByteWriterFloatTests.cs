using Gal.Core;
using Xunit;

public class SpanByteWriterFloatTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(-1f)]
    [InlineData(float.MaxValue)]
    public void WriteFloat(float value)
    {
        Span<byte> buffer = stackalloc byte[8];
        var span = buffer;

        span.WriteFloat(value);

        var expected = BitConverter.GetBytes(value);

#if BIGENDIAN
        Array.Reverse(expected);
#endif

        Assert.Equal(expected, buffer[..4].ToArray());
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-1d)]
    [InlineData(double.MaxValue)]
    public void WriteDouble(double value)
    {
        Span<byte> buffer = stackalloc byte[16];
        var span = buffer;

        span.WriteDouble(value);

        var expected = BitConverter.GetBytes(value);

#if BIGENDIAN
        Array.Reverse(expected);
#endif

        Assert.Equal(expected, buffer[..8].ToArray());
    }
}
