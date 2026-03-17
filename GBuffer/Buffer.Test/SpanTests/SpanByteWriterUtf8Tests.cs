using Gal.Core;
using Xunit;

public class SpanByteWriterUtf8Tests
{
    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("你好")]
    [InlineData("こんにちは")]
    public void WriteUtf8_ShouldRoundtrip(string value)
    {
        Span<byte> buffer = stackalloc byte[256];
        var span = buffer;

        span.WriteUtf8(value);

        ReadOnlySpan<byte> readSpan = buffer;

        string result = readSpan.ReadUtf8();

        Assert.Equal(value, result);
    }

    // [Fact]
    // public void WriteUtf8_BufferTooSmall_ShouldThrow()
    // {
    //     Span<byte> buffer = stackalloc byte[4];
    //     var span = buffer;
    //
    //     Assert.Throws<ArgumentOutOfRangeException>(() =>
    //         span.WriteUtf8("this is too long"));
    // }
}
