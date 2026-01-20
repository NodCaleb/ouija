using OuijaDesk.Application.DTO;
using OuijaDesk.Contracts.Models;

namespace OuijaDesk.Application.Contracts;

public interface IDeviceClient
{
    Task<DeviceStatusDto> CheckStatusAsync();

    Task<TransferResultDto> SendAsync(DeviceCommand command);
}
