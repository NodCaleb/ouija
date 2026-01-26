using OuijaDesk.Protocol.Encoding;
using OuijaDesk.Protocol.Checksum;
using OuijaDesk.Contracts.Models;
using OuijaDesk.Application.Contracts;
using Xunit;

namespace OuijaDesk.Tests;

public class ProtocolEncoderTests
{
    [Fact]
    public void Encode_NoPayload_ProducesExpectedFrame()
    {
        // Arrange
        var checksum = new XorChecksum();
        IProtocolEncoder encoder = new ProtocolEncoder(checksum);

        var command = new DeviceCommand
        {
            CommandType = 0x04, // DISPLAY_YES
            Message = null
        };

        // Act
        var frame = encoder.Encode(command);

        // Assert
        // Expected frame: AA 55 01 04 00 05
        Assert.Equal(6, frame.Length);
        Assert.Equal(0xAA, frame[0]);
        Assert.Equal(0x55, frame[1]);
        Assert.Equal(0x01, frame[2]);
        Assert.Equal(0x04, frame[3]);
        Assert.Equal(0x00, frame[4]);
        // Checksum
        Assert.Equal((byte)0x05, frame[5]);
    }

    [Fact]
    public void Encode_WithPayload_ProducesExpectedUppercasedAsciiPayloadAndChecksum()
    {
        // Arrange
        var checksum = new XorChecksum();
        IProtocolEncoder encoder = new ProtocolEncoder(checksum);

        var command = new DeviceCommand
        {
            CommandType = 0x01, // PLAY_SEQUENCE_ONCE
            Message = "abC123"
        };

        // Act
        var frame = encoder.Encode(command);

        // Assert
        // Frame layout: AA 55 01 01 06 41 42 43 31 32 33 XX
        Assert.Equal(6 + 6, frame.Length);
        Assert.Equal(0xAA, frame[0]);
        Assert.Equal(0x55, frame[1]);
        Assert.Equal(0x01, frame[2]);
        Assert.Equal(0x01, frame[3]);
        Assert.Equal(0x06, frame[4]);

        // Payload should be uppercased ASCII
        var payload = frame.Skip(5).Take(6).ToArray();
        Assert.Equal(new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'1', (byte)'2', (byte)'3' }, payload);

        // Verify checksum matches XOR of bytes 2..(last-1)
        byte checksumValue = 0;
        for (int i = 2; i < frame.Length - 1; i++)
            checksumValue ^= frame[i];

        Assert.Equal(checksumValue, frame[^1]);
    }

    [Fact]
    public void Encode_MessageTooLarge_ThrowsProtocolException()
    {
        // Arrange
        var checksum = new XorChecksum();
        IProtocolEncoder encoder = new ProtocolEncoder(checksum);

        var longMessage = new string('A', 300); // exceeds 255
        var command = new DeviceCommand
        {
            CommandType = 0x01,
            Message = longMessage
        };

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => encoder.Encode(command));
    }
}
