using OuijaDesk.Application.Contracts;
using OuijaDesk.Contracts.Models;
using OuijaDesk.Application.Exceptions;

namespace OuijaDesk.Protocol.Validation;

public class ProtocolValidator : IProtocolValidator
{
    public bool Validate(DeviceResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        return response.ResponseStatus == 0;
    }
}
