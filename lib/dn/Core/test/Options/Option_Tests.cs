using Xunit;

namespace NeoBeard.Options.Tests;

public class Option_Tests
{
    [Fact]
    public void Option_DefaultCtor_ReturnsNone()
    {
        Option<string> option = new();

        Assert.True(option.HasNoValue);
        Assert.False(option.HasValue);
        Assert.IsType<None>(option.Value);
    }

    [Fact]
    public void Option_Constructors_CreateExpectedState()
    {
        var fromCtorValue = new Option<int>(42);
        var fromSome = new Option<int>(new Some<int>(18));
        var fromFactory = Option<int>.Some(7);

        Assert.True(fromCtorValue.HasValue);
        Assert.True(fromSome.HasValue);
        Assert.True(fromFactory.HasValue);
        Assert.Equal(42, fromCtorValue.Value);
        Assert.Equal(18, fromSome.Value);
        Assert.Equal(7, fromFactory.Value);
    }

    [Fact]
    public void Option_SomeAndNoneProperties_WorkAsExpected()
    {
        Assert.True(Option<string>.Some("x").HasValue);
        Assert.True(Option<string>.NoneValue.HasNoValue);
    }

    [Fact]
    public void Option_From_ConvertsNullToNone()
    {
        var fromNull = Option<string>.From(null);
        var fromValue = Option<string>.From("x");

        Assert.True(fromNull.HasNoValue);
        Assert.True(fromValue.HasValue);
        Assert.Equal("x", fromValue.ValueOrDefault());
    }

    [Fact]
    public void Option_ImplicitConversions_ToOption_WorkFromMarkers()
    {
        Option<string> fromNone = None.Value;
        Option<string> fromNever = Never.Value;
        Option<string> fromDbNull = DBNull.Value;
        Option<string> fromTuple = new ValueTuple();

        Assert.True(fromNone.HasNoValue);
        Assert.True(fromNever.HasNoValue);
        Assert.True(fromDbNull.HasNoValue);
        Assert.True(fromTuple.HasNoValue);
    }

    [Fact]
    public void Option_ImplementsIOptionInterface()
    {
        IOption fromOption = Option<int>.Some(11);
        IOption noneOption = Option<int>.None();

        Assert.True(fromOption.HasValue);
        Assert.Equal(11, fromOption.Value);
        Assert.True(noneOption.HasNoValue);
        Assert.IsType<None>(noneOption.Value);
    }

    [Fact]
    public void Option_Inspect_InvokesOnlyWhenValueExists()
    {
        var inspected = 0;

        var none = Option<int>.None();
        var some = Option<int>.Some(5);

        none.Inspect(_ => inspected++);
        some.Inspect(_ => inspected += 5);

        Assert.Equal(5, inspected);
    }

    [Fact]
    public void Option_Map_MapsWhenValueExistsOtherwiseUsesFallbacks()
    {
        var some = Option<int>.Some(2);
        var none = Option<int>.None();

        Assert.Equal(3, some.Map(v => v + 1));
        Assert.Equal(0, none.Map(v => v + 1));
        Assert.Equal("factory", none.Map(_ => "value", () => "factory"));
        Assert.Equal("default", none.Map(_ => "value", "default"));
    }

    [Fact]
    public void Option_Match_ReturnsExpectedBoolean()
    {
        var some = Option<int>.Some(2);
        var none = Option<int>.None();

        Assert.True(some.Match(v => v == 2));
        Assert.False(none.Match(v => v == 2));
    }

    [Fact]
    public void Option_Or_ReturnsSelfOrFallback()
    {
        var some = Option<string>.Some("a");
        var none = Option<string>.None();

        Assert.Equal("a", some.Or("z").ValueOrDefault());
        Assert.Equal("z", none.Or("z").ValueOrDefault());
        Assert.Equal("f", none.Or(() => "f").ValueOrDefault());
    }

    [Fact]
    public void Option_TryGetValue_ReturnsCorrectStateAndValue()
    {
        var some = Option<string>.Some("value");
        var none = Option<string>.None();

        Assert.True(some.TryGetValue(out var someValue));
        Assert.Equal("value", someValue);

        Assert.False(none.TryGetValue(out var noneValue));
        Assert.Null(noneValue);
    }

    [Fact]
    public void Option_ValueOrDefault_UsesFallbacksWhenNone()
    {
        var some = Option<int>.Some(9);
        var none = Option<int>.None();

        Assert.Equal(9, some.ValueOrDefault());
        Assert.Equal(0, none.ValueOrDefault());
        Assert.Equal(99, none.ValueOrDefault(() => 99));
        Assert.Equal(10, none.ValueOrDefault(10));
    }

    [Fact]
    public void Option_ValueOrThrow_ThrowsOnlyWhenNone()
    {
        var some = Option<int>.Some(12);
        var none = Option<int>.None();

        Assert.Equal(12, some.ValueOrThrow());

        var exception = Assert.Throws<OptionException>(() => none.ValueOrThrow());
        Assert.Contains("none", exception.Message, StringComparison.Ordinal);

        var error = Assert.Throws<InvalidOperationException>(() => none.ValueOrThrow(() => new InvalidOperationException("missing")));
        Assert.Equal("missing", error.Message);

        var fromMessage = Assert.Throws<OptionException>(() => none.ValueOrThrow("custom"));
        Assert.Equal("custom", fromMessage.Message);
        var fromMessageFactory = Assert.Throws<OptionException>(() => none.ValueOrThrow(() => "custom from factory"));
        Assert.Equal("custom from factory", fromMessageFactory.Message);
    }

    [Fact]
    public void Option_ToResult_ConvertsToSuccessOrFailure()
    {
        var success = Option<int>.Some(1).ToResult();
        var failure = Option<int>.NoneValue.ToResult();

        Assert.True(success.HasValue);
        Assert.Equal(1, success.ValueOrDefault());

        Assert.True(failure.HasError);
        Assert.IsType<OptionException>(failure.ErrorOrDefault().Exception);
        Assert.Contains("none", failure.ErrorOrDefault().Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Some_Conversions_WorkToOptionAndValue()
    {
        Some<int> some = 13;
        Option<int> option = some;

        Assert.Equal(13, some.Value);
        Assert.Equal(13, option.ValueOrThrow());
        Assert.Equal(13, (int)some);

        var text = new Some<string>("text");
        Assert.Equal("text", text.ToString());
    }

    [Fact]
    public void None_IsNone_ForKnownMarkersAndOptions()
    {
        Assert.True(None.IsNone(None.Value));
        Assert.True(None.IsNone(Never.Value));
        Assert.True(None.IsNone(DBNull.Value));
        Assert.True(None.IsNone(new ValueTuple()));
        Assert.True(None.IsNone(Option<string>.None()));
        Assert.True(None.IsNone(ValueOption<int>.None()));
        Assert.False(None.IsNone("value"));
    }
}