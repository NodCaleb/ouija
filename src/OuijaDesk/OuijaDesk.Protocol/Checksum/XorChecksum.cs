namespace OuijaDesk.Protocol.Checksum;

public class XorChecksum : IChecksumCalculator
{
    public byte CalculateXorChecksum(ReadOnlySpan<byte> data)
    {
        byte checksum = 0;
        for (int i = 0; i < data.Length; i++)
            checksum ^= data[i];
        return checksum;
    }
}
