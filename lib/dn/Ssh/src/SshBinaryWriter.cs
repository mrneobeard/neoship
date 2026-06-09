using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace NeoBeard.Ssh;

internal static class SshBinaryWriter
{
    public static byte[] String(string value)
    {
        return String(Encoding.UTF8.GetBytes(value));
    }

    public static byte[] String(byte[] value)
    {
        return Concat(UInt32(checked((uint)value.Length)), value);
    }

    public static byte[] Mpint(byte[] value)
    {
        var normalized = NormalizeUnsigned(value);
        if (normalized.Length == 0)
            normalized = [0];

        if ((normalized[0] & 0x80) != 0)
            normalized = Concat([0], normalized);

        return String(normalized);
    }

    public static byte[] UInt32(uint value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return bytes;
    }

    public static byte[] Concat(params byte[][] arrays)
    {
        var length = arrays.Sum(static a => a.Length);
        var output = new byte[length];
        var offset = 0;
        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, output, offset, array.Length);
            offset += array.Length;
        }

        return output;
    }

    public static byte[] Mpint(BigInteger value)
    {
        return Mpint(value.ToByteArray(isUnsigned: true, isBigEndian: true));
    }

    private static byte[] NormalizeUnsigned(byte[] value)
    {
        var offset = 0;
        while (offset < value.Length - 1 && value[offset] == 0)
            offset++;

        return offset == 0 ? value : value[offset..];
    }
}