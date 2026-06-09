namespace NeoBeard.Secrets.Tests;

public class SecretGeneratorTests
{
    [Fact]
    public void Generate_WithDefaultValidator_ShouldMeetRequirements()
    {
        var generator = new SecretGenerator().AddDefaults();
        var secret = generator.GenerateAsString(12);

        Assert.Equal(12, secret.Length);
        Assert.Contains(secret, char.IsUpper);
        Assert.Contains(secret, char.IsLower);
        Assert.Contains(secret, char.IsDigit);
        Assert.Contains(secret, c => !char.IsLetterOrDigit(c));
    }

    [Fact]
    public void Generate_WithCustomValidator_ShouldRespectValidator()
    {
        var generator = new SecretGenerator()
            .Add(SecretCharacterSets.Digits)
            .SetValidator(chars => chars.All(char.IsDigit) && chars.Length == 6);

        var secret = generator.GenerateAsString(6);

        Assert.Equal(6, secret.Length);
        Assert.True(secret.All(char.IsDigit));
    }

    [Fact]
    public void Generate_WithEmptyCharacters_ShouldThrow()
    {
        var generator = new SecretGenerator();
        Assert.Throws<ArgumentNullException>(() => generator.Generate(10));
    }

    [Fact]
    public void Generate_WithInvalidLength_ShouldThrow()
    {
        var generator = new SecretGenerator().Add('a');
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(0));
    }

    [Fact]
    public void GenerateAsSecureString_ShouldReturnPopulatedSecureString()
    {
        var generator = new SecretGenerator().Add('a').SetValidator(_ => true);
        using var secure = generator.GenerateAsSecureString(5);

        Assert.Equal(5, secure.Length);
    }
}