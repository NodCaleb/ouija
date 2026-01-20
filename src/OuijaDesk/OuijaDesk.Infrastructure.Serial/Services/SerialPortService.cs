using OuijaDesk.Application.Contracts;
using OuijaDesk.Contracts.Models;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;

namespace OuijaDesk.Infrastructure.Serial.Services;

public class SerialPortService : ISerialPortService
{
    public IReadOnlyList<SerialPortInfo> GetAvailablePorts()
    {
        var names = System.IO.Ports.SerialPort.GetPortNames();
        var list = new List<SerialPortInfo>(names.Length);

        foreach (var name in names.OrderBy(n => n))
        {
            var info = new SerialPortInfo
            {
                PortName = name,
                Description = name
            };

            list.Add(info);
        }

        return list;
    }
}
