namespace Telemetria;

/// <summary>
/// バイト列に対する署名の生成と検証を担います。
/// </summary>
public interface IRequestSigner
{
    /// <summary>
    /// ペイロードに署名し、Base64 エンコードされた署名文字列を返します。
    /// 署名を行わない実装は空文字列を返します。
    /// </summary>
    string Sign(ReadOnlySpan<byte> payload);

    /// <summary>ペイロードと署名文字列が一致するかどうかを検証します。</summary>
    bool Verify(ReadOnlySpan<byte> payload, string signature);
}
