namespace OuijaDesk.Contracts.Models;

public class SerialPortInfo
{
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-friendly description or display name for the port.
    /// When not available it will contain the same value as <see cref="PortName"/>.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
