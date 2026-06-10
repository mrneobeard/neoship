using Xunit;

namespace NeoBeard.Options.Tests;

public class ValueOption_Tests
{
    [Fact]
    public void ValueOption_DefaultCtor_IsNone()
    {
        var option = new ValueOption<string>();

        Assert.True(option.HasNoValue);
        Assert.False(option.HasValue);
        Assert.Null(option.Value);
    }

    [Fact]
    public void ValueOption_ConstructorsAndFactories_SetState()
    {
        var byValue = new ValueOption<int>(33);
        var byFactory = ValueOption<int>.Some(44);
        var fromValue = (ValueOption<int>)55;
        var fromFrom = ValueOption<int>.None();
        var none = ValueOption<int>.NoneValue;

        Assert.True(byValue.HasValue);
        Assert.True(byFactory.HasValue);
        Assert.True(fromValue.HasValue);
        Assert.True(fromFrom.HasNoValue);
        Assert.True(none.HasNoValue);
        Assert.Equal(33, byValue.ValueOrDefault());
        Assert.Equal(44, byFactory.ValueOrDefault());
        Assert.Equal(55, fromValue.ValueOrDefault());
    }

    [Fact]
    public void ValueOption_MarkerConversions_WorkAsNone()
    {
        ValueOption<int> fromNone = None.Value;
        ValueOption<int> fromNever = Never.Value;
        ValueOption<int> fromDbNull = DBNull.Value;
        ValueOption<int> fromTuple = new ValueTuple();

        Assert.True(fromNone.HasNoValue);
        Assert.True(fromNever.HasNoValue);
        Assert.True(fromDbNull.HasNoValue);
        Assert.True(fromTuple.HasNoValue);
    }

    [Fact]
    public void ValueOption_ImplementsIOptionAndValuePayloadIsNullWhenNone()
    {
        IOption option = ValueOption<int>.Some(9);

        Assert.True(option.HasValue);
        Assert.Equal(9, option.Value);
        Assert.Null(ValueOption<string>.None().Value);
    }

    [Fact]
    public void ValueOption_Equality_ComparesStateAndValue()
    {
        var left = ValueOption<int>.Some(4);
        var right = ValueOption<int>.Some(4);
        var different = ValueOption<int>.Some(5);
        var none = ValueOption<int>.None();

        Assert.True(left.Equals(right));
        Assert.True(none.Equals(ValueOption<int>.None()));
        Assert.False(left.Equals(different));
        Assert.False(left.Equals(none));
        Assert.True(left.Equals(4));
        Assert.False(left.Equals(5));
    }

    [Fact]
    public void ValueOption_InspectAndMatch_WorkByState()
    {
        var none = ValueOption<string>.NoneValue;
        var some = ValueOption<string>.Some("x");

        var seen = 0;
        none.Inspect(_ => seen++);
        some.Inspect(_ => seen += 2);

        Assert.Equal(2, seen);
        Assert.True(some.Match(v => v == "x"));
        Assert.False(none.Match(v => v == "x"));
    }

    [Fact]
    public void ValueOption_Map_ReturnsExpectedValueOrFallback()
    {
        var some = ValueOption<int>.Some(3);
        var none = ValueOption<int>.NoneValue;

        Assert.Equal(6, some.Map(v => v * 2));
        Assert.Equal(0, none.Map(v => v * 2));
        Assert.Equal("fallback", none.Map(_ => "mapped", () => "fallback"));
        Assert.Equal("default", none.Map(_ => "mapped", "default"));
    }

    [Fact]
    public void ValueOption_Or_AndTryGetValue_WorkAsExpected()
    {
        var some = ValueOption<string>.Some("ok");
        var none = ValueOption<string>.None();

        Assert.Equal("ok", some.Or("else").ValueOrDefault());
        Assert.Equal("else", none.Or("else").ValueOrDefault());

        Assert.True(some.TryGetValue(out var someValue));
        Assert.Equal("ok", someValue);

        Assert.False(none.TryGetValue(out var noneValue));
        Assert.Null(noneValue);
    }

    [Fact]
    public void ValueOption_ValueOrDefault_UsesOverloads()
    {
        var some = ValueOption<int>.Some(8);
        var none = ValueOption<int>.None();

        Assert.Equal(8, some.ValueOrDefault());
        Assert.Equal(8, some.ValueOrDefault(() => 10));
        Assert.Equal(8, some.ValueOrDefault(10));

        Assert.Equal(0, none.ValueOrDefault());
        Assert.Equal(11, none.ValueOrDefault(() => 11));
        Assert.Equal(12, none.ValueOrDefault(12));
    }

    [Fact]
    public void ValueOption_ValueOrThrow_FollowsExpectedBehavior()
    {
        var some = ValueOption<int>.Some(1);
        var none = ValueOption<int>.None();

        Assert.Equal(1, some.ValueOrThrow());
        Assert.IsType<OptionException>(Assert.Throws<OptionException>(() => none.ValueOrThrow()));
        Assert.Equal("custom", Assert.Throws<OptionException>(() => none.ValueOrThrow("custom")).Message);
        Assert.Equal("from-factory", Assert.Throws<OptionException>(() => none.ValueOrThrow(() => "from-factory")).Message);
        Assert.Throws<InvalidOperationException>(() => none.ValueOrThrow(() => new InvalidOperationException("boom")));
    }
}