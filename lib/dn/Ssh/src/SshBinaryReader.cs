using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace NeoBeard.Ssh;

internal sealed class SshBinaryReader
{
    private readonly byte[] buffer;
    private int offset;

    public SshBinaryReader(byte[] buffer)
    {
        this.buffer = buffer;
    }

    public ReadOnlySpan<byte> Remaining => this.buffer.AsSpan(this.offset);

    public uint ReadUInt32()
    {
        if (this.offset + 4 > this.buffer.Length)
            throw new FormatException("Unexpected end of SSH binary data.");

        var value = BinaryPrimitives.ReadUInt32BigEndian(this.buffer.AsSpan(this.offset, 4));
        this.offset += 4;
        return value;
    }

    public string ReadString()
    {
        return Encoding.UTF8.GetString(this.ReadStringBytes());
    }

    public byte[] ReadStringBytes()
    {
        var length = checked((int)this.ReadUInt32());
        if (this.offset + length > this.buffer.Length)
            throw new FormatException("Unexpected end of SSH binary data.");

        var value = this.buffer.AsSpan(this.offset, length).ToArray();
        this.offset += length;
        return value;
    }

    public byte[] ReadMpintBytes()
    {
        var value = this.ReadStringBytes();
        if (value.Length > 1 && value[0] == 0)
            value = value[1..];

        return value;
    }

    public BigInteger ReadMpint()
    {
        return new BigInteger(this.ReadMpintBytes(), isUnsigned: true, isBigEndian: true);
    }
}