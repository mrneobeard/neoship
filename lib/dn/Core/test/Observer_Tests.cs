namespace NeoBeard.Tests;

public class Observer_Tests
{
    [Fact]
    public void Create_InvokesCallbacks()
    {
        int? nextValue = null;
        Exception? onError = null;
        var completed = false;

        var observer = Observer<int>.Create(
            onNext: value => nextValue = value,
            onError: ex => onError = ex,
            onCompleted: () => completed = true);

        var error = new InvalidOperationException("boom");
        observer.OnNext(42);
        observer.OnError(error);
        observer.OnCompleted();

        Assert.Equal(42, nextValue);
        Assert.Same(error, onError);
        Assert.True(completed);
    }

    [Fact]
    public void Methods_WithNullCallbacks_DoNotThrow()
    {
        var observer = new Observer<string>(null, null, null);

        observer.OnNext("value");
        observer.OnError(new Exception("ignored"));
        observer.OnCompleted();
    }
}