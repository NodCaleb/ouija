using System;
using System.Collections.Generic;
using System.Text;

namespace OuijaMobile.Core.Models;

public enum ConnectionState
{
    Disconnected = 0,
    Scanning = 1,
    Connecting = 2,
    Connected = 3,
    Disconnecting = 4
}
