using System;
using OuijaDesk.Application.Contracts;
using OuijaDesk.Application.DTO;
using OuijaDesk.Contracts.Models;

namespace OuijaDesk.Application.Services;

public class DeviceClient : IDeviceClient
{
    private readonly IProtocolEncoder _encoder;
    private readonly ITransport _transport;
    private readonly IProtocolDecoder _decoder;
    private readonly IProtocolValidator _validator;

    public DeviceClient(
        IProtocolEncoder encoder,
        ITransport transport,
        IProtocolDecoder decoder,
        IProtocolValidator validator)
    {
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public Task<DeviceStatusDto> CheckStatusAsync()
    {
        throw new NotImplementedException();
    }

    public Task<TransferResultDto> SendAsync(DeviceCommand command)
    {
        throw new NotImplementedException();
    }
}
