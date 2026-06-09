namespace NeoBeard.DotEnv;

internal enum TokenKind
{
    None = 0,
    Comment = 1,
    Name = 2,
    Value = 3,
    Newline = 4,
}

internal enum QuoteState
{
    None = 0,
    Single = 1,
    Double = 2,
    Backtick = 3,
}