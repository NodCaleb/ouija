using OuijaDesk.Contracts.Models;

namespace OuijaDesk.Application.Contracts;

public interface IProtocolValidator
{
    bool Validate(DeviceResponse response);
}
