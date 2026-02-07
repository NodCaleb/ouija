using OuijaDesk.Protocol.Decoding;
using Xunit;

namespace OuijaDesk.Tests;

public class ProtocolDecoderTests
{
    [Fact]
    public void Decode_NullInput_ThrowsArgumentException()
    {
        var decoder = new ProtocolDecoder();

        Assert.Throws<ArgumentException>(() => decoder.Decode(null!));
    }

    [Fact]
    public void Decode_EmptyInput_ThrowsArgumentException()
    {
        var decoder = new ProtocolDecoder();

        Assert.Throws<ArgumentException>(() => decoder.Decode(Array.Empty<byte>()));
    }

    [Fact]
    public void Decode_ValidBytes_ReturnsResponseWithFirstByteAsStatus()
    {
        var decoder = new ProtocolDecoder();
        var bytes = new byte[] { 0x05, 0xAA, 0x55 };

        var response = decoder.Decode(bytes);

        Assert.NotNull(response);
        Assert.Equal((byte)0x05, response.ResponseStatus);
    }
}