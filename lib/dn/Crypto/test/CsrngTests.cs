namespace NeoBeard.Crypto.Tests;

public class CsrngTests
{
    [Fact]
    public void NextBytes_Returns_Correct_Length()
    {
        using var rng = new Csrng();
        var bytes = rng.NextBytes(32);
        Assert.Equal(32, bytes.Length);
    }

    [Fact]
    public void NextBytes_Returns_Empty_For_Zero_Length()
    {
        using var rng = new Csrng();
        var bytes = rng.NextBytes(0);
        Assert.Empty(bytes);
    }

    [Fact]
    public void NextBytes_Throws_For_Negative_Length()
    {
        using var rng = new Csrng();
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextBytes(-1));
    }

    [Fact]
    public void NextBytes_Produces_Random_Values()
    {
        using var rng = new Csrng();
        var bytes1 = rng.NextBytes(32);
        var bytes2 = rng.NextBytes(32);
        Assert.NotEqual(bytes1, bytes2);
    }

    [Fact]
    public void GetBytes_Fills_Buffer()
    {
        using var rng = new Csrng();
        var buffer = new byte[32];
        rng.GetBytes(buffer);
        Assert.Contains(buffer, b => b != 0);
    }

    [Fact]
    public void GetBytes_Span_Fills_Buffer()
    {
        using var rng = new Csrng();
        Span<byte> buffer = stackalloc byte[32];
        rng.GetBytes(buffer);
        Assert.Contains(buffer.ToArray(), b => b != 0);
    }

    [Fact]
    public void NextInt16_Returns_Value()
    {
        using var rng = new Csrng();
        var value = rng.NextInt16();
        Assert.True(value >= short.MinValue && value <= short.MaxValue);
    }

    [Fact]
    public void NextInt32_Returns_Value()
    {
        using var rng = new Csrng();
        var value = rng.NextInt32();
        Assert.True(value >= int.MinValue && value <= int.MaxValue);
    }

    [Fact]
    public void NextInt64_Returns_Value()
    {
        using var rng = new Csrng();
        var value = rng.NextInt64();
        Assert.True(value >= long.MinValue && value <= long.MaxValue);
    }

    [Fact]
    public void GetNonZeroBytes_Produces_Non_Zero_Values()
    {
        using var rng = new Csrng();
        var buffer = new byte[100];
        rng.GetNonZeroBytes(buffer);
        Assert.All(buffer, b => Assert.NotEqual((byte)0, b));
    }

    [Fact]
    public void Dispose_Then_Use_Throws_ObjectDisposedException()
    {
        var rng = new Csrng();
        rng.Dispose();
        Assert.Throws<ObjectDisposedException>(() => rng.NextBytes(32));
    }

    [Fact]
    public void Dispose_Can_Be_Called_Multiple_Times()
    {
        var rng = new Csrng();
        rng.Dispose();
        rng.Dispose();
    }
}