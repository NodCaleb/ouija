using OuijaDesk.Contracts.Models;

namespace OuijaDesk.Application.Contracts;

public interface IProtocolValidator
{
    void Validate(DeviceResponse response);
}
