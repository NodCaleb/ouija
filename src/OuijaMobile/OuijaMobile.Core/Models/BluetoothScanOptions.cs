// MyBluetoothApp.Core/Models/BluetoothScanOptions.cs
using System;
using System.Collections.Generic;

namespace OuijaMobile.Core.Models;

public sealed class BluetoothScanOptions
{
    /// <summary>
    /// Scan duration hint. Implementations may ignore and rely on cancellation token.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// If set, only return devices whose Name contains this string (case-insensitive).
    /// </summary>
    public string? NameContains { get; init; }

    /// <summary>
    /// If set, only return devices advertising at least one of these services (BLE).
    /// Implementations may ignore for Classic BT.
    /// </summary>
    public IReadOnlyList<Guid> ServiceUuids { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// If true, allow duplicates to be yielded. Default false.
    /// </summary>
    public bool AllowDuplicates { get; init; } = false;

    /// <summary>
    /// If true, include already paired/bonded devices (when supported).
    /// </summary>
    public bool IncludePairedDevices { get; init; } = true;
}
