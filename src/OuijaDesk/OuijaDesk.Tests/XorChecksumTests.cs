using OuijaDesk.Protocol.Checksum;
using Xunit;

namespace OuijaDesk.Tests;

public class XorChecksumTests
{
    [Fact]
    public void CalculateXorChecksum_EmptySpan_ReturnsZero()
    {
        var calc = new XorChecksum();
        var data = Array.Empty<byte>();

        var result = calc.CalculateXorChecksum(data);

        Assert.Equal((byte)0x00, result);
    }

    [Fact]
    public void CalculateXorChecksum_SingleByte_ReturnsThatByte()
    {
        var calc = new XorChecksum();
        var data = new byte[] { 0xAB };

        var result = calc.CalculateXorChecksum(data);

        Assert.Equal((byte)0xAB, result);
    }

    [Fact]
    public void CalculateXorChecksum_MultipleBytes_ComputesXor()
    {
        var calc = new XorChecksum();
        var data = new byte[] { 0x01, 0x02, 0x03, 0xFF };

        // 0x01 ^ 0x02 = 0x03; 0x03 ^ 0x03 = 0x00; 0x00 ^ 0xFF = 0xFF
        var expected = (byte)0xFF;

        var result = calc.CalculateXorChecksum(data);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateXorChecksum_WithSpanSlice_OperatesOnProvidedRange()
    {
        var calc = new XorChecksum();
        var buffer = new byte[] { 0x00, 0x01, 0x04, 0x00, 0xFF };

        // Use slice [1..4] => 0x01, 0x04, 0x00 -> checksum = 0x05 (protocol example)
        var span = buffer.AsSpan(1, 3);
        var result = calc.CalculateXorChecksum(span);

        Assert.Equal((byte)0x05, result);
    }
}