// MyBluetoothApp.Core/Exceptions/BluetoothException.cs
using System;

namespace OuijaMobile.Core.Exceptions;

public class BluetoothException : Exception
{
    public BluetoothException() { }
    public BluetoothException(string message) : base(message) { }
    public BluetoothException(string message, Exception inner) : base(message, inner) { }
}
