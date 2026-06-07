namespace NeoBeard.Age.Tests;

public static class AgeCodecTests
{
    [Fact]
    public static void AgeBase64_Encodes_Without_Padding_And_Decodes()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };

        var encoded = AgeBase64.Encode(bytes);
        var decoded = AgeBase64.Decode(encoded);

        Assert.DoesNotContain('=', encoded);
        Assert.Equal(bytes, decoded);
    }

    [Fact]
    public static void AgeBase64_Rejects_Padded_Invalid_Length_And_NonCanonical_Values()
    {
        Assert.Throws<FormatException>(() => AgeBase64.Decode("AQ=="));
        Assert.Throws<FormatException>(() => AgeBase64.Decode("A"));
        Assert.Throws<FormatException>(() => AgeBase64.Decode("AB"));
    }

    [Fact]
    public static void Bech32_RoundTrips_And_Lowercases_Hrp()
    {
        var bytes = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

        var encoded = Bech32.Encode("AGE", bytes);
        var (hrp, decoded) = Bech32.Decode(encoded.ToUpperInvariant());

        Assert.StartsWith("age1", encoded);
        Assert.Equal("age", hrp);
        Assert.Equal(bytes, decoded);
    }

    [Fact]
    public static void Bech32_Rejects_Invalid_Forms()
    {
        var encoded = Bech32.Encode("age", new byte[] { 1, 2, 3 });
        var tampered = encoded[..^1] + (encoded[^1] == 'q' ? 'p' : 'q');

        Assert.Throws<ArgumentNullException>(() => Bech32.Decode(null!));
        Assert.Throws<FormatException>(() => Bech32.Decode(string.Empty));
        Assert.Throws<FormatException>(() => Bech32.Decode("Age1mixedcase"));
        Assert.Throws<FormatException>(() => Bech32.Decode("age1invalid!"));
        Assert.Throws<FormatException>(() => Bech32.Decode(tampered));
    }
}