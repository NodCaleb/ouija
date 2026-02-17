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
    public void Encode_WithPayload_ProducesExpectedPayloadAndChecksum()
    {
        // Arrange
        var checksum = new XorChecksum();
        IProtocolEncoder encoder = new ProtocolEncoder(checksum);

        // Test message: "123АБВ"
        // 1 -> 0x01, 2 -> 0x02, 3 -> 0x03
        // А -> 0x0A, Б -> 0x0B, В -> 0x0C
        var messageBytes = new byte[] { 0x01, 0x02, 0x03, 0x0A, 0x0B, 0x0C };

        var command = new DeviceCommand
        {
            CommandType = 0x01, // PLAY_SEQUENCE_ONCE
            Message = messageBytes
        };

        // Act
        var frame = encoder.Encode(command);

        // Assert
        // Frame layout: AA 55 01 01 06 01 02 03 0A 0B 0C XX
        Assert.Equal(6 + 6, frame.Length);
        Assert.Equal(0xAA, frame[0]);
        Assert.Equal(0x55, frame[1]);
        Assert.Equal(0x01, frame[2]);
        Assert.Equal(0x01, frame[3]);
        Assert.Equal(0x06, frame[4]);

        // Payload should match the input byte array
        var payload = frame.Skip(5).Take(6).ToArray();
        Assert.Equal(messageBytes, payload);

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

        var longMessage = new byte[300]; // exceeds 255
        var command = new DeviceCommand
        {
            CommandType = 0x01,
            Message = longMessage
        };

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => encoder.Encode(command));
    }
}
