namespace Telemetria;

/// <summary>
/// 時刻基準のワンタイムパスワード (RFC 6238 TOTP) を生成および検証する処理を表します。
/// </summary>
public interface IOneTimePasswordProvider
{
    /// <summary>現在時刻に対するワンタイムパスワードを生成します。</summary>
    string Generate();

    /// <summary>指定時刻に対するワンタイムパスワードを生成します。</summary>
    string GenerateAt(DateTimeOffset timestamp);

    /// <summary>現在時刻を基準にワンタイムパスワードを検証します。</summary>
    bool Validate(string code);

    /// <summary>指定時刻を基準にワンタイムパスワードを検証します。</summary>
    bool Validate(string code, DateTimeOffset timestamp);
}
