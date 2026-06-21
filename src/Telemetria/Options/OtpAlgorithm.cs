namespace Telemetria;

/// <summary>
/// ワンタイムパスワードに用いるハッシュアルゴリズムを表します。
/// </summary>
public enum OtpAlgorithm
{
    /// <summary>HMAC-SHA1 を用います。</summary>
    Sha1,

    /// <summary>HMAC-SHA256 を用います。</summary>
    Sha256,

    /// <summary>HMAC-SHA512 を用います。</summary>
    Sha512
}
