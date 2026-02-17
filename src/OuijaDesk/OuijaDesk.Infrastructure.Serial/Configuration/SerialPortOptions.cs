using System.IO.Ports;

namespace OuijaDesk.Infrastructure.Serial.Configuration;

/// <summary>
/// Default serial port options used by the serial transport implementation.
/// Values are intentionally mutable to allow configuration at application startup.
/// </summary>
internal static class SerialPortOptions
{
    public static int BaudRate { get; set; } = 115200;
    public static Parity Parity { get; set; } = Parity.None;
    public static int DataBits { get; set; } = 8;
    public static StopBits StopBits { get; set; } = StopBits.One;
    public static Handshake Handshake { get; set; } = Handshake.None;

    /// <summary>
    /// Read timeout in milliseconds used when reading from the serial port.
    /// A value of 0 indicates an infinite timeout.
    /// </summary>
    public static int ReadTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Write timeout in milliseconds used when writing to the serial port.
    /// A value of 0 indicates an infinite timeout.
    /// </summary>
    public static int WriteTimeoutMs { get; set; } = 1000;
}
