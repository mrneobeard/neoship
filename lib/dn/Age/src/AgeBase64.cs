namespace NeoBeard.Age;

internal static class AgeBase64
{
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=');
    }

    public static byte[] Decode(string value)
    {
        if (value.Contains('=', StringComparison.Ordinal))
            throw new FormatException("Padded base64 is not valid in age headers.");

        var padded = (value.Length % 4) switch
        {
            0 => value,
            2 => value + "==",
            3 => value + "=",
            _ => throw new FormatException("The base64 value has invalid length."),
        };

        var bytes = Convert.FromBase64String(padded);
        if (!string.Equals(Encode(bytes), value, StringComparison.Ordinal))
            throw new FormatException("The base64 value is not canonical.");

        return bytes;
    }
}