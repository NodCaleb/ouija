using OuijaMobile.Core.Contracts;
using OuijaMobile.Core.Exceptions;
using OuijaMobile.Core.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using System.Runtime.CompilerServices;

namespace OuijaMobile.App.Platforms.Android.Services;

public sealed class AndroidBleBluetoothService : IBluetoothService
{
    private readonly IBluetoothLE _ble;
    private readonly IAdapter _adapter;

    private readonly Guid _serviceUuid;
    private readonly Guid _writeCharacteristicUuid;

    private IDevice? _device;
    private ICharacteristic? _writeCharacteristic;

    private ConnectionInfo _connection = ConnectionInfo.Disconnected();

    public ConnectionInfo Connection => _connection;

    public event EventHandler<ConnectionInfo>? ConnectionChanged;

    public AndroidBleBluetoothService(Guid serviceUuid, Guid writeCharacteristicUuid)
    {
        _serviceUuid = serviceUuid;
        _writeCharacteristicUuid = writeCharacteristicUuid;

        _ble = CrossBluetoothLE.Current;
        _adapter = CrossBluetoothLE.Current.Adapter;

        _adapter.DeviceDisconnected += (_, __) => SetConnection(ConnectionInfo.Disconnected("Disconnected"));
        _adapter.DeviceConnectionLost += (_, __) => SetConnection(ConnectionInfo.Disconnected("Connection lost"));
    }

    public async IAsyncEnumerable<BluetoothDeviceInfo> ScanAsync(
        BluetoothScanOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureBluetoothAvailable();

        SetConnection(new ConnectionInfo { State = ConnectionState.Scanning });

        // Plugin.BLE delivers results via event; we expose them as async stream
        var channel = System.Threading.Channels.Channel.CreateUnbounded<BluetoothDeviceInfo>();

        void Handler(object? s, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            try
            {
                var d = e.Device;
                var info = new BluetoothDeviceInfo(d.Id.ToString())
                {
                    Name = d.Name,
                    Rssi = d.Rssi,
                    IsPaired = false, // Plugin.BLE doesn't always expose bonding cleanly
                    IsConnectable = true,
                    LastSeenUtc = DateTimeOffset.UtcNow
                };

                if (!string.IsNullOrWhiteSpace(options?.NameContains) &&
                    (info.Name?.IndexOf(options!.NameContains, StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
                {
                    return;
                }

                channel.Writer.TryWrite(info);
            }
            catch
            {
                // ignore scan mapping errors
            }
        }

        _adapter.DeviceDiscovered += Handler;

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (options?.Duration is not null)
                linkedCts.CancelAfter(options.Duration.Value);

            var scanTask = _adapter.StartScanningForDevicesAsync(
                serviceUuids: options?.ServiceUuids?.Count > 0 ? options.ServiceUuids.ToArray() : null,
                cancellationToken: linkedCts.Token);

            _ = Task.Run(async () =>
            {
                try { await scanTask.ConfigureAwait(false); }
                catch { /* ignored */ }
                finally { channel.Writer.TryComplete(); }
            }, CancellationToken.None);

            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return item;
        }
        finally
        {
            _adapter.DeviceDiscovered -= Handler;

            try { await _adapter.StopScanningForDevicesAsync().ConfigureAwait(false); }
            catch { /* ignore */ }

            // Do not force state to disconnected; scanning is not a connection state.
            if (_connection.State == ConnectionState.Scanning)
                SetConnection(ConnectionInfo.Disconnected());
        }
    }

    public async Task ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        EnsureBluetoothAvailable();

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("deviceId is required.", nameof(deviceId));

        SetConnection(new ConnectionInfo
        {
            Device = new BluetoothDeviceInfo(deviceId),
            State = ConnectionState.Connecting
        });

        try
        {
            var guid = Guid.Parse(deviceId);

            _device = await _adapter.ConnectToKnownDeviceAsync(guid, cancellationToken: cancellationToken)
                                    .ConfigureAwait(false);

            // Locate service + characteristic for writing
            var service = await _device.GetServiceAsync(_serviceUuid).ConfigureAwait(false);
            if (service is null)
                throw new BluetoothException($"Service not found: {_serviceUuid}");

            _writeCharacteristic = await service.GetCharacteristicAsync(_writeCharacteristicUuid).ConfigureAwait(false);
            if (_writeCharacteristic is null)
                throw new BluetoothException($"Write characteristic not found: {_writeCharacteristicUuid}");

            SetConnection(ConnectionInfo.Connected(new BluetoothDeviceInfo(_device.Id.ToString())
            {
                Name = _device.Name,
                Rssi = _device.Rssi,
                LastSeenUtc = DateTimeOffset.UtcNow
            }));
        }
        catch (DeviceConnectionException ex)
        {
            CleanupConnection();
            throw new BluetoothException("Failed to connect to device.", ex);
        }
        catch (Exception ex)
        {
            CleanupConnection();
            throw new BluetoothException("Failed to connect (unexpected error).", ex);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_device is null)
        {
            SetConnection(ConnectionInfo.Disconnected());
            return;
        }

        SetConnection(ConnectionInfo.Disconnecting(new BluetoothDeviceInfo(_device.Id.ToString())
        {
            Name = _device.Name
        }));

        try
        {
            await _adapter.DisconnectDeviceAsync(_device).ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }
        finally
        {
            CleanupConnection();
            SetConnection(ConnectionInfo.Disconnected());
        }
    }

    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        if (_device is null || _writeCharacteristic is null || !_connection.IsConnected)
            throw new BluetoothException("Not connected.");

        if (payload.Length == 0)
            return;

        // Conservative default chunk size. Many BLE paths handle larger, but 20 is safe.
        var max = await TryGetMaxWriteLengthAsync(cancellationToken).ConfigureAwait(false) ?? 20;

        var offset = 0;
        while (offset < payload.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var size = Math.Min(max, payload.Length - offset);
            var chunk = payload.Slice(offset, size).ToArray();

            // WriteWithResponse is safer; if your ESP32 expects WriteWithoutResponse,
            // you can adjust based on characteristic properties.
            var written = await _writeCharacteristic.WriteAsync(chunk).ConfigureAwait(false);
            // Some implementations return number of bytes written (int). Treat 0 as failure.
            if (written == 0)
                throw new BluetoothException("Write failed.");

            offset += size;
        }
    }

    public Task<int?> TryGetMaxWriteLengthAsync(CancellationToken cancellationToken = default)
    {
        // Plugin.BLE does not expose MTU reliably across all devices/versions.
        // Returning 20 keeps you safe. You can increase after testing (e.g., 100/180).
        return Task.FromResult<int?>(20);
    }

    public ValueTask DisposeAsync()
    {
        CleanupConnection();
        return ValueTask.CompletedTask;
    }

    private void EnsureBluetoothAvailable()
    {
        if (!_ble.IsAvailable)
            throw new BluetoothException("Bluetooth not available on this device.");
        if (!_ble.IsOn)
            throw new BluetoothException("Bluetooth is off.");
    }

    private void SetConnection(ConnectionInfo info)
    {
        _connection = info;
        ConnectionChanged?.Invoke(this, info);
    }

    private void CleanupConnection()
    {
        _writeCharacteristic = null;
        _device = null;
    }
}
