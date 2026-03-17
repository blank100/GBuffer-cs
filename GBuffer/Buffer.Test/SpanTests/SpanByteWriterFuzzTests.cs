using Gal.Core;
using Xunit;

public class SpanByteWriterFuzzTests
{
    [Fact]
    public void VarInt_RandomRoundTrip()
    {
        var rand = new Random(12345);

        for (int i = 0; i < 10000; i++)
        {
            int value = rand.Next();

            Span<byte> buffer = stackalloc byte[16];
            var span = buffer;

            span.WriteVarInt32(value);

            ReadOnlySpan<byte> readSpan = buffer;

            int result = readSpan.ReadVarInt32();

            Assert.Equal(value, result);
        }
    }
}
