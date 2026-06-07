using System.Security.Cryptography;

namespace NeoBeard.Crypto.Tests;

public class CompositeKeyTests
{
    [Fact]
    public void Constructor_Sets_Default_HashAlgorithm()
    {
        using var key = new CompositeKey();
        Assert.Equal(HashAlgorithmName.SHA256, key.HashAlgorithm);
    }

    [Fact]
    public void Constructor_With_HashAlgorithm_Sets_Property()
    {
        using var key = new CompositeKey(HashAlgorithmName.SHA512);
        Assert.Equal(HashAlgorithmName.SHA512, key.HashAlgorithm);
    }

    [Fact]
    public void Count_Is_Zero_By_Default()
    {
        using var key = new CompositeKey();
        Assert.Equal(0, key.Count);
    }

    [Fact]
    public void Add_Increments_Count()
    {
        using var key = new CompositeKey();
        key.Add(new TestFragment(new byte[] { 1, 2, 3 }));
        Assert.Equal(1, key.Count);
    }

    [Fact]
    public void Remove_Decrements_Count()
    {
        using var key = new CompositeKey();
        var fragment = new TestFragment(new byte[] { 1, 2, 3 });
        key.Add(fragment);
        key.Remove(fragment);
        Assert.Equal(0, key.Count);
    }

    [Fact]
    public void Clear_Resets_Count()
    {
        using var key = new CompositeKey();
        key.Add(new TestFragment(new byte[] { 1, 2, 3 }));
        key.Add(new TestFragment(new byte[] { 4, 5, 6 }));
        key.Clear();
        Assert.Equal(0, key.Count);
    }

    [Fact]
    public void AssembleKey_Returns_Hash_Size_Bytes()
    {
        using var key = new CompositeKey(HashAlgorithmName.SHA256);
        key.Add(new TestFragment(new byte[] { 1, 2, 3 }));
        var assembled = key.AssembleKey();
        Assert.Equal(32, assembled.Length);
    }

    [Fact]
    public void AssembleKey_Returns_NonEmpty_Bytes()
    {
        using var key = new CompositeKey();
        key.Add(new TestFragment(new byte[] { 1, 2, 3 }));
        key.Add(new TestFragment(new byte[] { 4, 5, 6 }));
        var assembled = key.AssembleKey();
        Assert.Equal(32, assembled.Length);
    }

    [Fact]
    public void AssembleKey_Order_Matters()
    {
        using var key1 = new CompositeKey();
        key1.Add(new TestFragment(new byte[] { 1, 2, 3 }));
        key1.Add(new TestFragment(new byte[] { 4, 5, 6 }));

        using var key2 = new CompositeKey();
        key2.Add(new TestFragment(new byte[] { 4, 5, 6 }));
        key2.Add(new TestFragment(new byte[] { 1, 2, 3 }));

        var assembled1 = key1.AssembleKey();
        var assembled2 = key2.AssembleKey();
        Assert.NotEqual(assembled1.ToArray(), assembled2.ToArray());
    }

    [Fact]
    public void GetEnumerator_Returns_All_Fragments()
    {
        using var key = new CompositeKey();
        var fragment1 = new TestFragment(new byte[] { 1, 2, 3 });
        var fragment2 = new TestFragment(new byte[] { 4, 5, 6 });
        key.Add(fragment1);
        key.Add(fragment2);

        var count = 0;
        foreach (var fragment in key)
        {
            count++;
        }

        Assert.Equal(2, count);
    }

    private class TestFragment : ICompositeKeyFragment
    {
        private readonly byte[] data;

        public TestFragment(byte[] data)
        {
            this.data = data;
        }

        public ReadOnlySpan<byte> ToReadOnlySpan() => this.data;

        public void Dispose()
        {
        }
    }
}