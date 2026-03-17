using Gal.Core;
using Xunit;

public class SpanByteWriterVarIntTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(16384)]
    [InlineData(uint.MaxValue)]
    public void WriteVarUInt32_ShouldRoundtrip(uint value)
    {
        Span<byte> buffer = stackalloc byte[16];
        var span = buffer;

        span.WriteVarUInt32(value);

        ReadOnlySpan<byte> readSpan = buffer;

        uint result = readSpan.ReadVarUInt32();

        Assert.Equal(value, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void WriteVarInt32_ShouldRoundtrip(int value)
    {
        Span<byte> buffer = stackalloc byte[16];
        var span = buffer;

        span.WriteVarInt32(value);

        ReadOnlySpan<byte> readSpan = buffer;

        int result = readSpan.ReadVarInt32();

        Assert.Equal(value, result);
    }
}
