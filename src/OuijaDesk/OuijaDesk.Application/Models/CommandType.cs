namespace OuijaDesk.Protocol.Constants;

public static class CommandType
{
    public static byte CheckStatus => 0x00;
    public static byte PlayOnce => 0x01;
    public static byte PlayRepeat => 0x02;
    public static byte Stop => 0x03;
    public static byte Yes => 0x04;
    public static byte No => 0x05;
}
