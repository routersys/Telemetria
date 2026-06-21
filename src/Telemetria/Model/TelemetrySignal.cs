using System.Collections.ObjectModel;

namespace Telemetria;

/// <summary>
/// 収集される単一のテレメトリ信号を表す不変オブジェクトです。
/// </summary>
public sealed record TelemetrySignal
{
    /// <summary>信号を一意に識別する識別子です。</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>信号の分類です。</summary>
    public required SignalCategory Category { get; init; }

    /// <summary>信号の名称です。</summary>
    public required string Name { get; init; }

    /// <summary>信号の重大度です。</summary>
    public SignalSeverity Severity { get; init; } = SignalSeverity.Information;

    /// <summary>信号が発生した時刻です。</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>付随する文字列プロパティです。</summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; } = ReadOnlyDictionary<string, string>.Empty;

    /// <summary>付随する数値計測値です。</summary>
    public IReadOnlyDictionary<string, double> Measurements { get; init; } = ReadOnlyDictionary<string, double>.Empty;

    /// <summary>例外に関する信号の場合の例外スナップショットです。</summary>
    public ExceptionSnapshot? Exception { get; init; }
}
