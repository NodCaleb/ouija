using OuijaDesk.Application.Contracts;
using System.IO.Ports;
using OuijaDesk.Infrastructure.Serial.Configuration;

namespace OuijaDesk.Infrastructure.Serial.Serial;

public class SerialPortTransport : ITransport
{
    /// <summary>
    /// Transfer a sequence of bytes to the specified serial port and return the device response bytes.
    /// This method opens the port, writes the data, then reads until the read timeout elapses.
    /// </summary>
    /// <param name="portName">Name of the serial port, e.g. "COM3".</param>
    /// <param name="data">Bytes to send to the device.</param>
    /// <remarks>
    /// All other serial port settings (baud rate, parity, data bits, stop bits, handshake,
    /// and timeouts) are read from <see cref="SerialPortOptions"/>.
    /// </remarks>
    /// <returns>Response bytes from the device. Empty array when no response.</returns>
    public Task<byte[]> TransferAsync(
        string portName,
        byte[] data)
    {
        if (string.IsNullOrWhiteSpace(portName))
            throw new ArgumentNullException(nameof(portName));

        if (data is null)
            throw new ArgumentNullException(nameof(data));

        return Task.Run(() =>
        {
            var result = new List<byte>();

            var effectiveBaud = SerialPortOptions.BaudRate;
            var effectiveParity = SerialPortOptions.Parity;
            var effectiveDataBits = SerialPortOptions.DataBits;
            var effectiveStopBits = SerialPortOptions.StopBits;
            var effectiveHandshake = SerialPortOptions.Handshake;
            var effectiveReadTimeout = SerialPortOptions.ReadTimeoutMs;
            var effectiveWriteTimeout = SerialPortOptions.WriteTimeoutMs;

            using var port = new SerialPort(portName, effectiveBaud, effectiveParity, effectiveDataBits, effectiveStopBits)
            {
                ReadTimeout = effectiveReadTimeout,
                WriteTimeout = effectiveWriteTimeout,
                Handshake = effectiveHandshake
            };

            try
            {
                port.Open();
                // clear any leftover data
                try { port.DiscardInBuffer(); } catch { }
                try { port.DiscardOutBuffer(); } catch { }

                if (data.Length > 0)
                {
                    port.Write(data, 0, data.Length);
                }

                var buffer = new byte[1024];

                // Read until a TimeoutException occurs (no more data within ReadTimeout)
                while (true)
                {
                    try
                    {
                        int read = port.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            for (int i = 0; i < read; i++)
                                result.Add(buffer[i]);

                            // if there are no more bytes immediately available, allow a small gap
                            if (port.BytesToRead == 0)
                            {
                                // small pause to let device finish sending
                                Thread.Sleep(20);
                                if (port.BytesToRead == 0)
                                    break;
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        // read timeout -> assume transmission complete
                        break;
                    }
                }
            }
            finally
            {
                try { if (port.IsOpen) port.Close(); } catch { }
            }

            return result.ToArray();
        });
    }
}
