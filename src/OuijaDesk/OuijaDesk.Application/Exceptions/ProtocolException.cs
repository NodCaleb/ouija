using System;
using System.Collections.Generic;
using System.Text;

namespace OuijaDesk.Application.Exceptions;

internal class ProtocolException : Exception
{
    public ProtocolException()
    {
    }
    public ProtocolException(string message)
        : base(message)
    {
    }
    public ProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
