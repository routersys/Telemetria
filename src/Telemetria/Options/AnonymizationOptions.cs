namespace Telemetria;

/// <summary>
/// 匿名化の構成オプションです。
/// </summary>
public sealed class AnonymizationOptions
{
    /// <summary>値を秘匿するプロパティキーの集合です。大文字小文字は区別しません。</summary>
    public ISet<string> RedactedKeys { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>メールアドレスを秘匿するかどうかです。</summary>
    public bool RedactEmails { get; set; } = true;

    /// <summary>IP アドレスを秘匿するかどうかです。</summary>
    public bool RedactIpAddresses { get; set; } = true;

    /// <summary>ファイルパスからユーザー固有部分を取り除くかどうかです。</summary>
    public bool ScrubFilePaths { get; set; } = true;

    /// <summary>秘匿値のハッシュ化に用いるソルトです。</summary>
    public string HashSalt { get; set; } = "telemetria";
}
