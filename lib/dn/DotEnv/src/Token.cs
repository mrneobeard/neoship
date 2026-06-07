namespace NeoBeard.DotEnv;

internal readonly struct Mark
{
    public int Line { get; init; }

    public int Column { get; init; }
}

internal sealed class Token
{
    public TokenKind Kind { get; init; }

    public string RawValue { get; init; } = string.Empty;

    public QuoteState Quote { get; init; }

    public Mark Start { get; init; }

    public Mark End { get; init; }
}