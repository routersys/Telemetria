namespace Telemetria;

/// <summary>
/// ワンタイムパスワードの構成オプションです。
/// </summary>
public sealed class OneTimePasswordOptions
{
    /// <summary>共有シークレットを Base32 で表したものです。</summary>
    public string? SecretBase32 { get; set; }

    /// <summary>パスワードの桁数です。</summary>
    public int Digits { get; set; } = 6;

    /// <summary>パスワードの有効期間です。</summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>用いるハッシュアルゴリズムです。</summary>
    public OtpAlgorithm Algorithm { get; set; } = OtpAlgorithm.Sha1;

    /// <summary>検証時に前後何ステップまで許容するかを表します。</summary>
    public int ValidationWindowSteps { get; set; } = 1;
}
