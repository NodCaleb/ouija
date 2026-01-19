using System;
using System.Collections.Generic;
using System.Text;

namespace OuijaMobile.Core.Models;

/// <summary>
/// Represents the currently selected/connected device and connection state.
/// Handy as a single bindable model in ViewModels.
/// </summary>
public sealed class ConnectionInfo
{
    public BluetoothDeviceInfo? Device { get; init; }

    public ConnectionState State { get; init; } = ConnectionState.Disconnected;

    /// <summary>
    /// Optional last error message to display in UI (keep it user-friendly).
    /// </summary>
    public string? LastError { get; init; }

    public bool IsConnected => State == ConnectionState.Connected;

    public static ConnectionInfo Disconnected(string? error = null)
        => new() { State = ConnectionState.Disconnected, LastError = error };

    public static ConnectionInfo Connecting(BluetoothDeviceInfo device)
        => new() { Device = device, State = ConnectionState.Connecting };

    public static ConnectionInfo Connected(BluetoothDeviceInfo device)
        => new() { Device = device, State = ConnectionState.Connected };

    public static ConnectionInfo Disconnecting(BluetoothDeviceInfo device)
        => new() { Device = device, State = ConnectionState.Disconnecting };
}
