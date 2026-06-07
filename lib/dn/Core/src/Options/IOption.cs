
namespace NeoBeard.Options;

public interface IOption
{
    bool HasValue { get; }

    bool HasNoValue { get; }

    object? Value { get; }
}