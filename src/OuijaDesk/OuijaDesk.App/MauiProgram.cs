using Microsoft.Extensions.Logging;
using OuijaDesk.App.Pages;
using OuijaDesk.App.ViewModels;
using OuijaDesk.Application.Contracts;
using OuijaDesk.Application.Services;
using OuijaDesk.Infrastructure.Serial.Services;
using OuijaDesk.Infrastructure.Serial.Serial;
using OuijaDesk.Protocol.Checksum;
using OuijaDesk.Protocol.Encoding;
using OuijaDesk.Protocol.Decoding;
using OuijaDesk.Protocol.Validation;

namespace OuijaDesk.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Register Protocol Services
            builder.Services.AddSingleton<IChecksumCalculator, XorChecksum>();
            builder.Services.AddSingleton<IProtocolEncoder, ProtocolEncoder>();
            builder.Services.AddSingleton<IProtocolDecoder, ProtocolDecoder>();
            builder.Services.AddSingleton<IProtocolValidator, ProtocolValidator>();
            
            // Register Infrastructure Services
            builder.Services.AddSingleton<ITransport, SerialPortTransport>();
            builder.Services.AddSingleton<ISerialPortService, SerialPortService>();
            
            // Register Application Services
            builder.Services.AddSingleton<IDeviceClient, DeviceClient>();
            
            // Register ViewModels and Pages
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();

            return builder.Build();
        }
    }
}
