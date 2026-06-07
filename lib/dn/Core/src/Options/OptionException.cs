namespace NeoBeard.Options;

public class OptionException : Exception
{
    public OptionException()
    {
    }

    public OptionException(string message) : base(message)
    {
    }

    public object? Value { get; }

    public OptionException(string message, object? value) : base(message)
    {
        this.Value = value;
    }

    public OptionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public OptionException(string message, Exception innerException, object? value) : base(message, innerException)
    {
        this.Value = value;
    }
}