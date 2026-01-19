using System;
using System.Collections.Generic;
using System.Text;

namespace OuijaMobile.Core.Contracts;

/// <summary>
/// Optional persistence for UX (remember last connected device, etc.).
/// Implementation can use Preferences/SecureStorage depending on needs.
/// </summary>
public interface IDeviceRepository
{
    Task<string?> GetLastDeviceIdAsync(CancellationToken cancellationToken = default);
    Task SetLastDeviceIdAsync(string? deviceId, CancellationToken cancellationToken = default);
}