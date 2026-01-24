using OuijaDesk.Protocol.Constants;

namespace OuijaDesk.Contracts.Models;

public class DeviceCommand
{
    public byte CommandType { get; set; }
    public string Message { get; set; }
}
