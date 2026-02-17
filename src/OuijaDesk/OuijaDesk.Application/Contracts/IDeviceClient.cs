using OuijaDesk.Application.DTO;
using OuijaDesk.Contracts.Models;

namespace OuijaDesk.Application.Contracts;

public interface IDeviceClient
{
    Task<DeviceStatusDto> CheckStatusAsync(string portName);

    Task<TransferResultDto> SendAsync(string portName, DeviceCommand command);
}
