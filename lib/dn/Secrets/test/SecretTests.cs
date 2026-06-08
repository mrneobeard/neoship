namespace NeoBeard.Secrets.Tests;

public static class SecretTests
{
    [Fact]
    public static void ByteSecret_UnshroudInto_Copies_Original_Bytes()
    {
        byte[] value = [1, 2, 3, 4];
        using var secret = new Secret<byte>(value);
        Span<byte> destination = stackalloc byte[value.Length];

        secret.UnshroudInto(destination);

        Assert.Equal(value, destination.ToArray());
    }

    [Fact]
    public static void CharSecret_CopyTo_Copies_Original_Chars()
    {
        using var secret = new Secret<char>("password".AsSpan());
        Span<char> destination = stackalloc char[secret.Length];

        secret.CopyTo(destination);

        Assert.Equal("password", new string(destination));
    }

    [Fact]
    public static void Clone_Creates_Independent_Copy()
    {
        using var secret = new Secret<byte>([9, 8, 7]);
        using var clone = secret.Clone();

        secret.Dispose();

        Assert.Equal(new byte[] { 9, 8, 7 }, clone.ToUnshroudedArray());
    }

    [Fact]
    public static void Append_Value_Adds_Single_Element()
    {
        using var secret = new Secret<char>("pass".AsSpan());

        secret.Append('!');

        Assert.Equal(5, secret.Length);
        Assert.Equal("pass!", secret.ToUnshroudedString());
    }

    [Fact]
    public static void Append_Span_Adds_Multiple_Elements()
    {
        using var secret = new Secret<byte>([1, 2]);

        secret.Append([3, 4, 5]);

        Assert.Equal(5, secret.Length);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, secret.ToUnshroudedArray());
    }

    [Fact]
    public static void Append_Empty_Span_Does_Not_Change_Secret()
    {
        using var secret = new Secret<byte>([1, 2]);

        secret.Append([]);

        Assert.Equal(2, secret.Length);
        Assert.Equal(new byte[] { 1, 2 }, secret.ToUnshroudedArray());
    }

    [Fact]
    public static void ToString_Does_Not_Reveal_Secret()
    {
        using var secret = new Secret<char>("do-not-log".AsSpan());

        var text = secret.ToString();

        Assert.DoesNotContain("do-not-log", text, StringComparison.Ordinal);
        Assert.Equal("*******", text);
    }

    [Fact]
    public static void Span_Implicit_Conversion_Creates_Secret()
    {
        Span<byte> bytes = [1, 2, 3];
        using Secret<byte> secret = bytes;

        Assert.Equal(new byte[] { 1, 2, 3 }, secret.ToUnshroudedArray());
    }

    [Fact]
    public static void ReadOnlySpan_Implicit_Conversion_Creates_Secret()
    {
        ReadOnlySpan<char> chars = "token".AsSpan();
        using Secret<char> secret = chars;

        Assert.Equal("token", secret.ToUnshroudedString());
    }

    [Fact]
    public static void ToUnshroudedString_Returns_String()
    {
        using var secret = new Secret<char>("token".AsSpan());

        Assert.Equal("token", secret.ToUnshroudedString());
    }

    [Fact]
    public static void UnshroudAsString_Returns_Char_String()
    {
        using var secret = new Secret<char>("token".AsSpan());

        Assert.Equal("token", secret.UnshroudAsString());
    }

    [Fact]
    public static void UnshroudAsString_Returns_Utf8_Byte_String()
    {
        using var secret = new Secret<byte>("token"u8);

        Assert.Equal("token", secret.UnshroudAsString());
    }

    [Fact]
    public static void UnshroudAsArray_Returns_Array_Copy()
    {
        using var secret = new Secret<byte>([1, 2, 3]);

        var array = secret.UnshroudAsArray();
        array[0] = 9;

        Assert.Equal(new byte[] { 9, 2, 3 }, array);
        Assert.Equal(new byte[] { 1, 2, 3 }, secret.UnshroudAsArray());
    }

    [Fact]
    public static void UnshroudArraySegment_Returns_Full_Array_Segment()
    {
        using var secret = new Secret<char>("abc".AsSpan());

        var segment = secret.UnshroudArraySegment();

        Assert.Equal(0, segment.Offset);
        Assert.Equal(3, segment.Count);
        Assert.Equal(new[] { 'a', 'b', 'c' }, segment.Array);
    }

    [Fact]
    public static void Use_Provides_Temporary_Unshrouded_Span()
    {
        using var secret = new Secret<byte>([1, 2, 3]);
        byte sum = 0;

        secret.Use(0, static (span, _) => Assert.Equal(new byte[] { 1, 2, 3 }, span.ToArray()));
        secret.Use(0, (span, _) => sum = (byte)span.ToArray().Sum(static value => value));

        Assert.Equal(6, sum);
    }

    [Fact]
    public static void UnshroudInto_Throws_When_Destination_Is_Too_Small()
    {
        using var secret = new Secret<byte>([1, 2, 3]);
        byte[] destination = new byte[2];

        Assert.Throws<ArgumentException>(() => secret.UnshroudInto(destination));
    }

    [Fact]
    public static void Disposed_Secret_Throws()
    {
        var secret = new Secret<byte>([1]);
        secret.Dispose();

        Assert.Throws<ObjectDisposedException>(() => secret.Length);
        Assert.Throws<ObjectDisposedException>(() => secret.Append((byte)2));
        Assert.Throws<ObjectDisposedException>(() => secret.UnshroudInto(stackalloc byte[1]));
        Assert.Equal("*******", secret.ToString());
    }

    [Fact]
    public static void Unsupported_Element_Type_Throws()
    {
        Assert.Throws<NotSupportedException>(() => new Secret<int>([1]));
    }

    [Fact]
    public static void ValueSecret_Copies_Original_Contents()
    {
        var secret = new ValueSecret<byte>([1, 2, 3]);
        Span<byte> destination = stackalloc byte[secret.Length];

        secret.CopyTo(destination);

        Assert.Equal(new byte[] { 1, 2, 3 }, destination.ToArray());
    }

    [Fact]
    public static void ValueSecret_Append_Value_Returns_New_Secret()
    {
        var secret = new ValueSecret<char>("key".AsSpan());

        var appended = secret.Append('!');

        Assert.Equal(3, secret.Length);
        Assert.Equal(4, appended.Length);
        using var converted = appended.ToSecret();
        Assert.Equal("key!", converted.ToUnshroudedString());
    }

    [Fact]
    public static void ValueSecret_Append_Span_Returns_New_Secret()
    {
        var secret = new ValueSecret<byte>([1, 2]);

        var appended = secret.Append([3, 4]);

        using var originalSecret = secret.ToSecret();
        using var appendedSecret = appended.ToSecret();
        Assert.Equal(new byte[] { 1, 2 }, originalSecret.ToUnshroudedArray());
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, appendedSecret.ToUnshroudedArray());
    }

    [Fact]
    public static void ValueSecret_Converts_To_And_From_Secret()
    {
        using var secret = new Secret<byte>([9, 8]);

        ValueSecret<byte> valueSecret = secret;
        using Secret<byte> converted = valueSecret;

        Assert.Equal(new byte[] { 9, 8 }, converted.ToUnshroudedArray());
    }

    [Fact]
    public static void ValueSecret_ToString_Returns_Masked_Value()
    {
        var secret = new ValueSecret<char>("do-not-log".AsSpan());

        Assert.Equal("*******", secret.ToString());
    }

    [Fact]
    public static void ValueSecret_UnshroudAsString_Returns_Char_String()
    {
        var secret = new ValueSecret<char>("token".AsSpan());

        Assert.Equal("token", secret.UnshroudAsString());
    }

    [Fact]
    public static void ValueSecret_UnshroudAsString_Returns_Utf8_Byte_String()
    {
        var secret = new ValueSecret<byte>("token"u8);

        Assert.Equal("token", secret.UnshroudAsString());
    }

    [Fact]
    public static void ValueSecret_UnshroudAsArray_Returns_Array_Copy()
    {
        var secret = new ValueSecret<byte>([1, 2, 3]);

        var array = secret.UnshroudAsArray();
        array[0] = 9;

        Assert.Equal(new byte[] { 9, 2, 3 }, array);
        Assert.Equal(new byte[] { 1, 2, 3 }, secret.UnshroudAsArray());
    }

    [Fact]
    public static void ValueSecret_UnshroudArraySegment_Returns_Full_Array_Segment()
    {
        var secret = new ValueSecret<char>("abc".AsSpan());

        var segment = secret.UnshroudArraySegment();

        Assert.Equal(0, segment.Offset);
        Assert.Equal(3, segment.Count);
        Assert.Equal(new[] { 'a', 'b', 'c' }, segment.Array);
    }
}