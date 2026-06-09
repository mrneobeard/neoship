namespace NeoBeard.Tests;

public class Never_Tests
{
    [Fact]
    public void Value_EqualsDefault()
    {
        var value = Never.Value;

        Assert.Equal(default(Never), value);
    }
}