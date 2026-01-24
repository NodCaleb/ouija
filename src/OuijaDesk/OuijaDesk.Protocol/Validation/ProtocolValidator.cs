using OuijaDesk.Application.Contracts;
using OuijaDesk.Contracts.Models;
using OuijaDesk.Application.Exceptions;

namespace OuijaDesk.Protocol.Validation;

public class ProtocolValidator : IProtocolValidator
{
    public void Validate(DeviceResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        if (response.ResponseStatus != 0)
            throw new ProtocolException($"Invalid response status: {response.ResponseStatus}.");
    }
}
