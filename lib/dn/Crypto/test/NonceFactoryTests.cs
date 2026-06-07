namespace NeoBeard.Crypto.Tests;

public class NonceFactoryTests
{
    public NonceFactoryTests()
    {
        NonceFactory.Clear(clearArray: true);
    }

    [Fact]
    public void Generate_Returns_Correct_Size()
    {
        var nonce = NonceFactory.Generate(16);
        Assert.Equal(16, nonce.Length);
    }

    [Fact]
    public void Generate_Default_Size_Is_8()
    {
        var nonce = NonceFactory.Generate();
        Assert.Equal(8, nonce.Length);
    }

    [Fact]
    public void Generate_Returns_Unique_Values()
    {
        var nonce1 = NonceFactory.Generate();
        var nonce2 = NonceFactory.Generate();
        Assert.NotEqual(nonce1, nonce2);
    }

    [Fact]
    public void Remove_Returns_True_For_Existing_Nonce()
    {
        var nonce = NonceFactory.Generate();
        var result = NonceFactory.Remove(nonce);
        Assert.True(result);
    }

    [Fact]
    public void Remove_Returns_False_For_NonExisting_Nonce()
    {
        var nonce = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var result = NonceFactory.Remove(nonce);
        Assert.False(result);
    }

    [Fact]
    public void Clear_Removes_All_Nonces()
    {
        NonceFactory.Generate();
        NonceFactory.Generate();
        NonceFactory.Generate();

        NonceFactory.Clear();

        var nonce = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        Assert.False(NonceFactory.Remove(nonce));
    }

    [Fact]
    public void Generate_Does_Not_Produce_Duplicates()
    {
        var nonces = new List<byte[]>();
        for (int i = 0; i < 100; i++)
        {
            var nonce = NonceFactory.Generate(4);
            Assert.DoesNotContain(nonce, nonces, new ByteArrayComparer());
            nonces.Add(nonce);
        }
    }

    private class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x is null || y is null) return x == y;
            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj) => obj.Length;
    }
}