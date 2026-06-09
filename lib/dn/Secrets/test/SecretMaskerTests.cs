using System.Text;

namespace NeoBeard.Secrets.Tests;

public class SecretMaskerTests
{
    [Fact]
    public void Mask_ShouldRedactRegisteredSecrets()
    {
        var masker = new SecretMasker();
        masker.Add("password123");
        masker.Add("secret-key");

        string input = "The password is password123 and the key is secret-key.";
        string expected = "The password is ********** and the key is **********.";

        var result = masker.Mask(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Mask_WithDerivativeGenerator_ShouldRedactDerivatives()
    {
        var masker = new SecretMasker();
        masker.AddDerivativeGenerator(secret =>
        {
            var bytes = Encoding.UTF8.GetBytes(secret.ToString());
            return Convert.ToBase64String(bytes).AsMemory();
        });

        masker.Add("hello"); // Base64 is "aGVsbG8="

        string input = "Raw: hello, Base64: aGVsbG8=";
        string expected = "Raw: **********, Base64: **********";

        var result = masker.Mask(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Mask_WithEmptyInput_ShouldReturnOriginal()
    {
        var masker = new SecretMasker();
        masker.Add("secret");

        Assert.Null(masker.Mask((string?)null));
        Assert.Equal(string.Empty, masker.Mask(string.Empty));
        Assert.Equal("   ", masker.Mask("   "));
    }

    [Fact]
    public void Mask_WithNoSecrets_ShouldReturnOriginal()
    {
        var masker = new SecretMasker();
        string input = "no secrets here";
        Assert.Equal(input, masker.Mask(input));
    }

    [Fact]
    public void NullSecretMasker_ShouldNotMaskAnything()
    {
        var masker = NullSecretMasker.Default;
        masker.Add("secret");
        string input = "this is a secret";
        Assert.Equal(input, masker.Mask(input));
    }
}