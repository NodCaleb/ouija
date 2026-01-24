using OuijaDesk.Application.Contracts;
using OuijaDesk.Contracts.Models;

namespace OuijaDesk.Protocol.Decoding;

public class ProtocolDecoder : IProtocolDecoder
{
    public DeviceResponse Decode(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            throw new ArgumentException("Input byte array must contain at least one byte.", nameof(bytes));
        }

        return new DeviceResponse
        {
            ResponseStatus = bytes[0]
        };
    }
}
