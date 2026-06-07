namespace NeoBeard.Age;

internal static class Bech32
{
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
    private static readonly sbyte[] CharsetIndex = BuildCharsetIndex();

    public static string Encode(string hrp, ReadOnlySpan<byte> bytes)
    {
        ArgumentNullException.ThrowIfNull(hrp);
        var lower = hrp.ToLowerInvariant();
        var data = ConvertBits(bytes, 8, 5, true);
        var checksum = CreateChecksum(lower, data);

        var chars = new char[lower.Length + 1 + data.Length + checksum.Length];
        lower.CopyTo(0, chars, 0, lower.Length);
        var offset = lower.Length;
        chars[offset++] = '1';

        for (var i = 0; i < data.Length; i++)
            chars[offset + i] = Charset[data[i]];

        offset += data.Length;
        for (var i = 0; i < checksum.Length; i++)
            chars[offset + i] = Charset[checksum[i]];

        return new string(chars);
    }

    public static (string Hrp, byte[] Bytes) Decode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
            throw new FormatException("The Bech32 value is invalid.");

        var hasLower = false;
        var hasUpper = false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsWhiteSpace(c))
                throw new FormatException("The Bech32 value is invalid.");

            hasLower |= c >= 'a' && c <= 'z';
            hasUpper |= c >= 'A' && c <= 'Z';
        }

        if (hasLower && hasUpper)
            throw new FormatException("The Bech32 value mixes casing.");

        var lower = value.ToLowerInvariant();
        var separator = lower.LastIndexOf('1');
        if (separator < 1 || separator + 7 > lower.Length)
            throw new FormatException("The Bech32 separator is invalid.");

        var hrp = lower[..separator];
        var dataLength = lower.Length - separator - 1;
        var data = new byte[dataLength];

        for (var i = 0; i < dataLength; i++)
        {
            var valueIndex = DecodeCharset(lower[separator + 1 + i]);
            if (valueIndex < 0)
                throw new FormatException("The Bech32 data is invalid.");

            data[i] = (byte)valueIndex;
        }

        if (!VerifyChecksum(hrp, data))
            throw new FormatException("The Bech32 checksum is invalid.");

        return (hrp, ConvertBits(data.AsSpan(0, dataLength - 6), 5, 8, false));
    }

    private static byte[] ConvertBits(ReadOnlySpan<byte> data, int fromBits, int toBits, bool pad)
    {
        var acc = 0;
        var bits = 0;
        var maxv = (1 << toBits) - 1;
        var result = new byte[(data.Length * fromBits + toBits - 1) / toBits];
        var count = 0;

        foreach (var value in data)
        {
            if ((value >> fromBits) != 0)
                throw new FormatException("The Bech32 data contains an invalid value.");

            acc = (acc << fromBits) | value;
            bits += fromBits;
            while (bits >= toBits)
            {
                bits -= toBits;
                result[count++] = (byte)((acc >> bits) & maxv);
            }
        }

        if (pad)
        {
            if (bits > 0)
                result[count++] = (byte)((acc << (toBits - bits)) & maxv);
        }
        else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
        {
            throw new FormatException("The Bech32 padding is invalid.");
        }

        if (count == result.Length)
            return result;

        var exact = new byte[count];
        result.AsSpan(0, count).CopyTo(exact);
        return exact;
    }

    private static byte[] HrpExpand(string hrp)
    {
        var expanded = new byte[(hrp.Length * 2) + 1];
        for (var i = 0; i < hrp.Length; i++)
            expanded[i] = (byte)(hrp[i] >> 5);

        expanded[hrp.Length] = 0;
        for (var i = 0; i < hrp.Length; i++)
            expanded[hrp.Length + 1 + i] = (byte)(hrp[i] & 31);

        return expanded;
    }

    private static int Polymod(ReadOnlySpan<byte> hrp, ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<int> generator = [0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3];
        var chk = 1;
        for (var i = 0; i < hrp.Length; i++)
        {
            var top = chk >> 25;
            chk = ((chk & 0x1ffffff) << 5) ^ hrp[i];
            for (var j = 0; j < 5; j++)
            {
                if (((top >> j) & 1) != 0)
                    chk ^= generator[j];
            }
        }

        for (var i = 0; i < data.Length; i++)
        {
            var top = chk >> 25;
            chk = ((chk & 0x1ffffff) << 5) ^ data[i];
            for (var j = 0; j < 5; j++)
            {
                if (((top >> j) & 1) != 0)
                    chk ^= generator[j];
            }
        }

        return chk;
    }

    private static bool VerifyChecksum(string hrp, ReadOnlySpan<byte> data)
    {
        return Polymod(HrpExpand(hrp), data) == 1;
    }

    private static byte[] CreateChecksum(string hrp, ReadOnlySpan<byte> data)
    {
        var values = new byte[data.Length + 6];
        data.CopyTo(values);
        var polymod = Polymod(HrpExpand(hrp), values) ^ 1;
        var checksum = new byte[6];
        for (var i = 0; i < checksum.Length; i++)
            checksum[i] = (byte)((polymod >> (5 * (5 - i))) & 31);

        return checksum;
    }

    private static int DecodeCharset(char value)
    {
        return value < CharsetIndex.Length ? CharsetIndex[value] : -1;
    }

    private static sbyte[] BuildCharsetIndex()
    {
        var values = new sbyte[128];
        for (var i = 0; i < values.Length; i++)
            values[i] = -1;

        for (var i = 0; i < Charset.Length; i++)
            values[(byte)Charset[i]] = (sbyte)i;

        return values;
    }
}
