using OuijaDesk.Protocol.Constants;

namespace OuijaDesk.Contracts.Models;

public class DeviceCommand
{
    public byte CommandType { get; set; }
    public byte[]? Message { get; set; }
}
