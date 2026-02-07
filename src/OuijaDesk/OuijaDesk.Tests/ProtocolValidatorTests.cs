using OuijaDesk.Protocol.Validation;
using OuijaDesk.Contracts.Models;

namespace OuijaDesk.Tests;

public class ProtocolValidatorTests
{
    [Fact]
    public void Validate_Null_ThrowsArgumentNullException()
    {
        var validator = new ProtocolValidator();

        Assert.Throws<ArgumentNullException>(() => validator.Validate(null!));
    }

    [Fact]
    public void Validate_StatusZero_ReturnsTrue()
    {
        var validator = new ProtocolValidator();
        var response = new DeviceResponse { ResponseStatus = 0x00 };

        var result = validator.Validate(response);

        Assert.True(result);
    }

    [Fact]
    public void Validate_StatusNonZero_ReturnsFalse()
    {
        var validator = new ProtocolValidator();
        var response = new DeviceResponse { ResponseStatus = 0x05 };

        var result = validator.Validate(response);

        Assert.False(result);
    }
}