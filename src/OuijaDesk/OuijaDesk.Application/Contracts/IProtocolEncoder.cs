using OuijaDesk.Contracts.Models;

namespace OuijaDesk.Application.Contracts;

public interface IProtocolEncoder
{
    byte[] Encode(DeviceCommand command);
}
