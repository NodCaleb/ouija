using OuijaDesk.Application.Contracts;
using OuijaDesk.Application.Exceptions;
using OuijaDesk.Contracts.Models;
using OuijaDesk.Protocol.Constants;
using OuijaDesk.Protocol.Checksum;

namespace OuijaDesk.Protocol.Encoding;

public sealed class ProtocolEncoder : IProtocolEncoder
{
    private readonly IChecksumCalculator _checksumCalculator;

    // Assumptions:
    // - DeviceCommand has: byte CommandType, string? Message
    // - Message is encoded as UTF-8 bytes
    // - Payload length is 0..255 (1-byte length field)
    // - Checksum is XOR of all bytes except the checksum byte itself

    public ProtocolEncoder(IChecksumCalculator checksumCalculator)
    {
        _checksumCalculator = checksumCalculator ?? throw new ArgumentNullException(nameof(checksumCalculator));
    }

    public byte[] Encode(DeviceCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        // Null message => empty payload
        var message = command.Message ?? string.Empty;

        // Encode message to bytes (strict, deterministic)
        // Note: if you later need a fixed terminator (e.g., '\0') or fixed-length padding,
        // that belongs here, by protocol definition.
        var payload = System.Text.Encoding.ASCII.GetBytes(message.ToUpper());

        if (payload.Length > ProtocolConstants.MaxPayloadLength)
            throw new ProtocolException(
                $"Encoded message length {payload.Length} exceeds maximum allowed size {ProtocolConstants.MaxPayloadLength}.");

        // Frame layout:
        // [0]   Magic 0xAA
        // [1]   Magic 0x55
        // [2]   Protocol version
        // [3]   Command type
        // [4]   Payload length
        // [5..] Payload (UTF-8 message bytes)
        // [last] Checksum

        var frameLength = 6 + payload.Length;
        var buffer = new byte[frameLength];

        buffer[0] = ProtocolConstants.Magic1;
        buffer[1] = ProtocolConstants.Magic2;
        buffer[2] = ProtocolConstants.ProtocolVersion;
        buffer[3] = command.CommandType;
        buffer[4] = (byte)payload.Length;

        if (payload.Length > 0)
        {
            Buffer.BlockCopy(payload, 0, buffer, 5, payload.Length);
        }

        buffer[^1] = _checksumCalculator.CalculateXorChecksum(buffer.AsSpan(0, buffer.Length - 1));

        return buffer;
    }    
}

