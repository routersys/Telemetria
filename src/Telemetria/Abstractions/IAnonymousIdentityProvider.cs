namespace Telemetria;

/// <summary>
/// 個人を特定しない匿名識別子を提供する処理を表します。
/// </summary>
public interface IAnonymousIdentityProvider
{
    /// <summary>現在の匿名識別子を取得します。</summary>
    string Current { get; }

    /// <summary>匿名識別子を新しい値へ更新します。</summary>
    void Rotate();
}
