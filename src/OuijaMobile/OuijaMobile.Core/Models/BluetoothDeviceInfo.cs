using System;
using System.Collections.Generic;
using System.Text;

namespace OuijaMobile.Core.Models;

/// <summary>
/// Cross-platform, UI-friendly representation of a Bluetooth device discovered via scan.
/// Keep it library-agnostic (no plugin-specific types here).
/// </summary>
public sealed class BluetoothDeviceInfo : IEquatable<BluetoothDeviceInfo>
{
    /// <summary>
    /// A stable device identifier *as provided by your BT stack/library*.
    /// Examples: Guid (iOS), MAC-like string (Android), etc.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Human-readable name (may be null/empty for some devices).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Signal strength in dBm (typically negative). Null if not available.
    /// </summary>
    public int? Rssi { get; init; }

    /// <summary>
    /// True if your BT stack says the device is paired/bonded.
    /// </summary>
    public bool IsPaired { get; init; }

    /// <summary>
    /// True if the device was discovered via connectable advertising (if available).
    /// </summary>
    public bool IsConnectable { get; init; } = true;

    /// <summary>
    /// Service UUIDs advertised by the device (if available).
    /// </summary>
    public IReadOnlyList<Guid> ServiceUuids { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Manufacturer payload (raw). Null/empty if not available.
    /// </summary>
    public byte[]? ManufacturerData { get; init; }

    /// <summary>
    /// Optional platform-specific address (e.g., MAC on Android, if accessible).
    /// Leave null when unavailable or restricted by OS.
    /// </summary>
    public string? PlatformAddress { get; init; }

    /// <summary>
    /// Timestamp when last seen during scanning (UTC).
    /// Useful for expiring “stale” devices in the UI list.
    /// </summary>
    public DateTimeOffset LastSeenUtc { get; init; } = DateTimeOffset.UtcNow;

    public BluetoothDeviceInfo(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Device id must not be null or whitespace.", nameof(id));

        Id = id;
    }

    /// <summary>
    /// Friendly label for lists.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "(Unnamed device)" : Name!;

    /// <summary>
    /// Friendly RSSI label for UI.
    /// </summary>
    public string RssiDisplay => Rssi is null ? string.Empty : $"{Rssi} dBm";

    public override string ToString() => $"{DisplayName} [{Id}]";

    public bool Equals(BluetoothDeviceInfo? other)
        => other is not null && StringComparer.Ordinal.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is BluetoothDeviceInfo other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Id);
}

