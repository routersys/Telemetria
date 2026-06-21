using Telemetria.Collection;
using Xunit;

namespace Telemetria.Tests.Collection;

public sealed class ExceptionSnapshotFactoryTests
{
    [Fact]
    public void Create_CapturesTypeAndMessage()
    {
        var snapshot = ExceptionSnapshotFactory.Create(new InvalidOperationException("nope"));
        Assert.Equal("System.InvalidOperationException", snapshot.Type);
        Assert.Equal("nope", snapshot.Message);
    }

    [Fact]
    public void Create_CapturesStackTrace_WhenThrown()
    {
        ExceptionSnapshot snapshot;
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (Exception ex)
        {
            snapshot = ExceptionSnapshotFactory.Create(ex);
        }

        Assert.False(string.IsNullOrEmpty(snapshot.StackTrace));
    }

    [Fact]
    public void Create_CapturesInnerException()
    {
        var exception = new InvalidOperationException("outer", new ArgumentException("inner"));
        var snapshot = ExceptionSnapshotFactory.Create(exception);

        Assert.NotNull(snapshot.Inner);
        Assert.Equal("System.ArgumentException", snapshot.Inner!.Type);
        Assert.Equal("inner", snapshot.Inner.Message);
    }

    [Fact]
    public void Create_CapturesAggregateInnerExceptions()
    {
        var aggregate = new AggregateException(new InvalidOperationException("a"), new ArgumentException("b"));
        var snapshot = ExceptionSnapshotFactory.Create(aggregate);

        Assert.Equal(2, snapshot.Aggregated.Count);
        Assert.Contains(snapshot.Aggregated, s => s.Message == "a");
        Assert.Contains(snapshot.Aggregated, s => s.Message == "b");
    }

    [Fact]
    public void Create_RespectsMaxDepth()
    {
        var deepest = new Exception("level3");
        var middle = new Exception("level2", deepest);
        var top = new Exception("level1", middle);

        var snapshot = ExceptionSnapshotFactory.Create(top, maxDepth: 2);

        Assert.NotNull(snapshot.Inner);
        Assert.Null(snapshot.Inner!.Inner);
    }
}
