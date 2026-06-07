namespace NeoBeard.Results;

public class ResultException : Exception
{
    public ResultException()
    {
    }

    public ResultException(string message) : base(message)
    {
    }

    public object? Value { get; }

    public ResultException(string message, object? value) : base(message)
    {
        this.Value = value;
    }

    public ResultException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ResultException(string message, Exception innerException, object? value) : base(message, innerException)
    {
        this.Value = value;
    }
}