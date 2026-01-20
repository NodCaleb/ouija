using OuijaDesk.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OuijaDesk.Application.Contracts;

public interface ISerialPortService
{
    IReadOnlyList<SerialPortInfo> GetAvailablePorts();
}
