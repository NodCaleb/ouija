using System;
using System.Collections.Generic;
using System.Text;

namespace OuijaMobile.Core.Contracts;

/// <summary>
/// Converts user input into a device-specific payload (bytes).
/// Keep protocol logic here, not in ViewModels.
/// </summary>
public interface IByteEncoder
{
    /// <summary>Encode text into bytes ready to send.</summary>
    byte[] Encode(string text);
}