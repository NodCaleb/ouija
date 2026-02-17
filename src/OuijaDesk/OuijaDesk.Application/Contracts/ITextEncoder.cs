namespace OuijaDesk.Application.Contracts;

public interface ITextEncoder
{
    byte[] Encode(string? text);
    string Decode(byte[]? bytes);
}
