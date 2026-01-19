// MyBluetoothApp.Core/Abstractions/IBluetoothService.cs
using OuijaMobile.Core.Models;

namespace OuijaMobile.Core.Contracts;

/// <summary>
/// Library-agnostic Bluetooth contract for scanning, connecting and sending bytes.
/// Implementation can be BLE (GATT) or Classic (SPP) depending on your target device.
/// </summary>
public interface IBluetoothService : IAsyncDisposable
{
    /// <summary>Current connection state.</summary>
    ConnectionInfo Connection { get; }

    /// <summary>Raised whenever Connection changes (state, device, error).</summary>
    event EventHandler<ConnectionInfo>? ConnectionChanged;

    /// <summary>
    /// Scan for devices. Emits devices as they are discovered (duplicates may occur).
    /// Caller is responsible for de-duplication (usually by Id) and updating UI.
    /// </summary>
    IAsyncEnumerable<BluetoothDeviceInfo> ScanAsync(
        BluetoothScanOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Connect to the specified device.</summary>
    Task ConnectAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>Disconnect if connected/connecting.</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a byte payload to the connected device.
    /// For BLE implementations, this typically writes to a known characteristic.
    /// </summary>
    Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Optional: some devices require an MTU or packet size-aware sender.
    /// If not supported by your implementation, return null or throw NotSupportedException.
    /// </summary>
    Task<int?> TryGetMaxWriteLengthAsync(CancellationToken cancellationToken = default);
}
