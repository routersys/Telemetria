namespace Telemetria.Collection;

/// <summary>
/// 例外を送信に適した不変のスナップショットへ変換します。
/// </summary>
public static class ExceptionSnapshotFactory
{
    private const int DefaultMaxDepth = 8;

    /// <summary>例外からスナップショットを生成します。</summary>
    public static ExceptionSnapshot Create(Exception exception, int maxDepth = DefaultMaxDepth)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return Build(exception, Math.Max(1, maxDepth));
    }

    private static ExceptionSnapshot Build(Exception exception, int depthRemaining)
    {
        var aggregated = exception is AggregateException aggregate && depthRemaining > 1
            ? aggregate.InnerExceptions.Select(e => Build(e, depthRemaining - 1)).ToArray()
            : [];

        var inner = exception.InnerException is { } innerException
                    && depthRemaining > 1
                    && exception is not AggregateException
            ? Build(innerException, depthRemaining - 1)
            : null;

        return new ExceptionSnapshot
        {
            Type = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            Inner = inner,
            Aggregated = aggregated
        };
    }
}
