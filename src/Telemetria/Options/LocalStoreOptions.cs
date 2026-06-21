namespace Telemetria;

/// <summary>
/// ローカル退避ストアの構成オプションです。
/// </summary>
public sealed class LocalStoreOptions
{
    /// <summary>退避先ディレクトリです。</summary>
    public string Directory { get; set; } = "telemetria-pending";

    /// <summary>保持できる退避項目の最大件数です。</summary>
    public int MaxItems { get; set; } = 1024;
}
