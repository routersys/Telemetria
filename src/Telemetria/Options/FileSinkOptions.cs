namespace Telemetria;

/// <summary>
/// ローカルファイルシンクの構成オプションです。
/// </summary>
public sealed class FileSinkOptions
{
    /// <summary>書き出し先ディレクトリです。</summary>
    public string Directory { get; set; } = "telemetria";

    /// <summary>ファイル名の接頭辞です。</summary>
    public string FileNamePrefix { get; set; } = "signals";
}
