using OuijaDesk.Protocol.Encoding;
using Xunit;

namespace OuijaDesk.Tests;

public class TextEncoderTests
{
    [Fact]
    public void Encode_Digits_ProducesExpectedBytes()
    {
        // Arrange
        var text = "0123456789";

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Encode(text);

        // Assert
        Assert.Equal(new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 }, result);
    }

    [Fact]
    public void Encode_CyrillicLetters_ProducesExpectedBytes()
    {
        // Arrange
        var text = "АБВ";

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Encode(text);

        // Assert
        // А -> 0x0A (10), Б -> 0x0B (11), В -> 0x0C (12)
        Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C }, result);
    }

    [Fact]
    public void Encode_MixedDigitsAndCyrillic_ProducesExpectedBytes()
    {
        // Arrange
        var text = "123АБВ";

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Encode(text);

        // Assert
        // 1 -> 0x01, 2 -> 0x02, 3 -> 0x03
        // А -> 0x0A, Б -> 0x0B, В -> 0x0C
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x0A, 0x0B, 0x0C }, result);
    }

    [Fact]
    public void Encode_LowercaseCyrillic_ConvertsToUppercase()
    {
        // Arrange
        var text = "абв";

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Encode(text);

        // Assert
        // Should be same as uppercase АБВ
        Assert.Equal(new byte[] { 0x0A, 0x0B, 0x0C }, result);
    }

    [Fact]
    public void Encode_EmptyString_ReturnsEmptyArray()
    {
        // Arrange
        var text = "";

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Encode(text);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Encode_Null_ReturnsEmptyArray()
    {
        // Act
        var encoder = new TextEncoder();
        var result = encoder.Encode(null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Encode_UnsupportedCharacter_ThrowsArgumentException()
    {
        // Arrange
        var text = "ABC"; // Latin letters

        // Act & Assert
        var encoder = new TextEncoder();
        Assert.Throws<ArgumentException>(() => encoder.Encode(text));
    }

    [Fact]
    public void Encode_LastCyrillicLetter_ProducesExpectedByte()
    {
        // Arrange
        var text = "Я"; // Last letter in the Cyrillic alphabet

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Encode(text);

        // Assert
        // "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" has 33 letters including Ё
        // Я is at index 32 (0-based), so byte value is 0x0A + 32 = 0x2A (42)
        Assert.Equal(new byte[] { 0x2A }, result);
    }

    [Fact]
    public void Encode_LetterYo_ProducesExpectedByte()
    {
        // Arrange
        var text = "Ё"; // Letter Ё comes after Е in the alphabet

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Encode(text);

        // Assert
        // Ё is at index 6 (after АБВГДЕ), so byte value is 0x0A + 6 = 0x10 (16)
        Assert.Equal(new byte[] { 0x10 }, result);
    }

    [Fact]
    public void Decode_Digits_ProducesExpectedString()
    {
        // Arrange
        var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Decode(bytes);

        // Assert
        Assert.Equal("0123456789", result);
    }

    [Fact]
    public void Decode_CyrillicBytes_ProducesExpectedString()
    {
        // Arrange
        var bytes = new byte[] { 0x0A, 0x0B, 0x0C };

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Decode(bytes);

        // Assert
        Assert.Equal("АБВ", result);
    }

    [Fact]
    public void Decode_MixedBytes_ProducesExpectedString()
    {
        // Arrange
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x0A, 0x0B, 0x0C };

        // Act
        var encoder = new TextEncoder();
        var result = encoder.Decode(bytes);

        // Assert
        Assert.Equal("123АБВ", result);
    }

    [Fact]
    public void Decode_EmptyArray_ReturnsEmptyString()
    {
        // Arrange
        var bytes = Array.Empty<byte>();

        // Act
        var result = TextEncoder.Decode(bytes);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Decode_Null_ReturnsEmptyString()
    {
        // Act
        var encoder = new TextEncoder();
        var result = encoder.Decode(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Decode_InvalidByte_ThrowsArgumentException()
    {
        // Arrange
        var bytes = new byte[] { 0xFF }; // Out of valid range

        // Act & Assert
        var encoder = new TextEncoder();
        Assert.Throws<ArgumentException>(() => encoder.Decode(bytes));
    }

    [Fact]
    public void EncodeAndDecode_RoundTrip_ProducesOriginalString()
    {
        // Arrange
        var originalText = "0123456789АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";

        // Act
        var encoder = new TextEncoder();
        var encoded = encoder.Encode(originalText);
        var decoded = encoder.Decode(encoded);

        // Assert
        Assert.Equal(originalText, decoded);
    }
}
