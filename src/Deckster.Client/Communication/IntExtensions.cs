namespace Deckster.Client.Communication;

public static class IntExtensions
{
    public static int ToInt(this byte[] bytes)
    {
        return bytes[0] |
               bytes[1] << 8 |
               bytes[2] << 16 |
               bytes[3] << 24;
    }

    public static byte[] ToBytes(this int length)
    {
        var bytes = new byte[4];
        bytes[3] = (byte) (length >> 24 & 0xff);
        bytes[2] = (byte) (length >> 16 & 0xff);
        bytes[1] = (byte) (length >> 8 & 0xff);
        bytes[0] = (byte) (length & 0xff);
        return bytes;
    }
}