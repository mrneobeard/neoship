using System.Text;

namespace NeoBeard.Age;

internal static class Pem
{
    public static byte[] Decode(string pem, string label)
    {
        var begin = "-----BEGIN " + label + "-----";
        var end = "-----END " + label + "-----";
        var beginIndex = pem.IndexOf(begin, StringComparison.Ordinal);
        var endIndex = pem.IndexOf(end, StringComparison.Ordinal);
        if (beginIndex < 0 || endIndex < beginIndex)
            throw new FormatException("The PEM block is missing.");

        var base64 = pem[(beginIndex + begin.Length) ..endIndex];
        var builder = new StringBuilder(base64.Length);
        foreach (var c in base64)
        {
            if (!char.IsWhiteSpace(c))
                builder.Append(c);
        }

        return Convert.FromBase64String(builder.ToString());
    }
}