using OuijaDesk.Contracts.Models;

namespace OuijaDesk.Application.Contracts;

public interface IProtocolDecoder
{
    DeviceResponse Decode(byte[] bytes);
}
