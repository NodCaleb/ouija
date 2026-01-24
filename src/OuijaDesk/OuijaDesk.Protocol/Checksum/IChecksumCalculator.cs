
namespace OuijaDesk.Protocol.Checksum;

public interface IChecksumCalculator
{
    byte CalculateXorChecksum(ReadOnlySpan<byte> data);
}
