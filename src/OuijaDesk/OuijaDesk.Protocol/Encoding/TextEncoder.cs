using OuijaDesk.Application.Contracts;

namespace OuijaDesk.Protocol.Encoding;

/// <summary>
/// Encodes text to custom byte array format:
/// - Digits 0-9 map to bytes 0x00-0x09
/// - Cyrillic letters А-Я map to bytes 0x0A onwards (33 letters total, including Ё)
/// </summary>
public class TextEncoder : ITextEncoder
{
    private const string CyrillicAlphabet = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";

    /// <summary>
    /// Encodes a string containing digits and Cyrillic letters to byte array.
    /// </summary>
    /// <param name="text">Input text (should contain only 0-9 and А-Я)</param>
    /// <returns>Encoded byte array</returns>
    /// <exception cref="ArgumentException">Thrown when text contains unsupported characters</exception>
    public byte[] Encode(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<byte>();

        var result = new byte[text.Length];

        for (int i = 0; i < text.Length; i++)
        {
            char c = char.ToUpper(text[i]);

            // Handle digits 0-9 => 0x00-0x09
            if (c >= '0' && c <= '9')
            {
                result[i] = (byte)(c - '0');
            }
            // Handle Cyrillic letters А-Я => 0x0A onwards
            else
            {
                int index = CyrillicAlphabet.IndexOf(c);
                if (index == -1)
                {
                    throw new ArgumentException($"Unsupported character '{c}' at position {i}. Only digits 0-9 and Cyrillic letters А-Я are supported.", nameof(text));
                }
                result[i] = (byte)(0x0A + index);
            }
        }

        return result;
    }

    /// <summary>
    /// Decodes a byte array back to string.
    /// </summary>
    /// <param name="bytes">Encoded byte array</param>
    /// <returns>Decoded string</returns>
    /// <exception cref="ArgumentException">Thrown when byte value is out of valid range</exception>
    public string Decode(byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        var result = new char[bytes.Length];

        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];

            // Handle 0x00-0x09 => digits 0-9
            if (b <= 0x09)
            {
                result[i] = (char)('0' + b);
            }
            // Handle 0x0A onwards => Cyrillic letters
            else
            {
                int index = b - 0x0A;
                if (index < 0 || index >= CyrillicAlphabet.Length)
                {
                    throw new ArgumentException($"Invalid byte value 0x{b:X2} at position {i}. Expected range: 0x00-0x{0x0A + CyrillicAlphabet.Length - 1:X2}.", nameof(bytes));
                }
                result[i] = CyrillicAlphabet[index];
            }
        }

        return new string(result);
    }
}
