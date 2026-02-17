using OuijaDesk.Application.Contracts;
using OuijaDesk.Application.DTO;
using OuijaDesk.Contracts.Models;
using OuijaDesk.Protocol.Constants;

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

    public async Task<DeviceStatusDto> CheckStatusAsync(string portName)
    {
        var command = new DeviceCommand
        {
            CommandType = CommandType.CheckStatus
        };

        var payload = _encoder.Encode(command);

        byte[] responseBytes;
        try
        {
            // Transport requires a port name; callers/configuration should ensure transport knows which port to use.
            // Passing empty string here allows test/mocked transports to accept the call. Concrete transports
            // may throw an ArgumentException which we treat as device offline/unavailable.
            responseBytes = await _transport.TransferAsync(portName, payload).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return new DeviceStatusDto { Online = false, Success = false, Message = $"Ошибка установки соединения: {e.Message}" };
        }

        if (responseBytes == null || responseBytes.Length == 0)
            return new DeviceStatusDto { Online = false, Success = false, Message = "Нет ответа от устройства." };

        var response = _decoder.Decode(responseBytes);

        var isValid = _validator.Validate(response);

        return new DeviceStatusDto { Online = isValid, Success = true };
    }

    public async Task<TransferResultDto> SendAsync(string portName, DeviceCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        byte[] payload;
        try
        {
            payload = _encoder.Encode(command);
        }
        catch (Exception ex)
        {
            return new TransferResultDto { Success = false, Message = $"Ошибка кодирования: {ex.Message}" };
        }

        byte[] responseBytes;
        try
        {
            responseBytes = await _transport.TransferAsync(portName, payload).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new TransferResultDto { Success = false, Message = $"Ошибка транспортного уровня: {ex.Message}" };
        }

        if (responseBytes == null || responseBytes.Length == 0)
            return new TransferResultDto { Success = false, Message = "Нет ответа от устройства." };

        DeviceResponse response;
        try
        {
            response = _decoder.Decode(responseBytes);
        }
        catch (Exception ex)
        {
            return new TransferResultDto { Success = false, Message = $"Ошибка декодирования: {ex.Message}" };
        }

        var isValid = _validator.Validate(response);

        if (isValid)
            return new TransferResultDto { Success = true };

        return new TransferResultDto { Success = false, Message = $"Неверный статус ответа: {response.ResponseStatus}." };
    }
}
