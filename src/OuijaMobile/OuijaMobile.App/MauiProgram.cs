using System;
using Microsoft.Extensions.Logging;
#if ANDROID
using OuijaMobile.App.Platforms.Android.Services;
#endif
using OuijaMobile.Core.Contracts;

namespace OuijaMobile.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var serviceUuid = Guid.Parse("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
        var writeCharUuid = Guid.Parse("yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy");

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if ANDROID
        builder.Services.AddSingleton<IBluetoothService>(_ =>
            new AndroidBleBluetoothService(serviceUuid, writeCharUuid));
#else
        // Register a fallback that will fail at runtime if used on non-Android platforms.
        builder.Services.AddSingleton<IBluetoothService>(_ =>
            throw new PlatformNotSupportedException("Android BLE implementation is not available on this platform."));
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
