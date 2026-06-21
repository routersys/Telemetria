namespace Telemetria;

/// <summary>
/// テレメトリの送出方式を表します。
/// </summary>
public enum TelemetriaMode
{
    /// <summary>送出を行いません。</summary>
    Disabled,

    /// <summary>リモートサーバーへ暗号化送信します。失敗時はローカルへ退避し再送します。</summary>
    Remote,

    /// <summary>サーバーを用いずローカルファイルへのみ書き出します。</summary>
    Local,

    /// <summary>リモート送信とローカル書き出しの双方を行います。</summary>
    Both
}
