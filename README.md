# Telemetria

疎結合で汎用的なテレメトリ収集・診断ライブラリです。利用状況の集積、例外の自動解析、ハイブリッド暗号化とワンタイムパスワードによる匿名送信、サーバー非依存のローカルフォールバックを提供します。プラグインなどに組み込み、発生した問題を自動で送信する用途を想定しています。

## 特長

- **疎結合 / SOLID**: すべての主要機能はインターフェースで抽象化され、差し替え可能です。
- **高度な暗号化**: 一時鍵による楕円曲線ディフィー・ヘルマン (P-256) と HKDF、AES-256-GCM を組み合わせたハイブリッド暗号化を採用しています。
- **匿名通信**: 個人を特定しない匿名識別子と、RFC 6238 (TOTP / Google Authenticator 互換) のワンタイムパスワードで送信します。
- **サーバー不要でも動作**: ローカルファイルへの書き出し、送信失敗時のローカル退避と自動再送に対応します。
- **拡張可能なパイプライン**: 重大度フィルタ、サンプリング、文脈付与、匿名化、任意の追加処理を順に適用します。

## 利用例

```csharp
services.AddTelemetria(builder =>
{
    builder.ConfigureCore(o =>
    {
        o.Mode = TelemetriaMode.Both;
        o.ApplicationName = "MyPlugin";
        o.ApplicationVersion = "1.2.3";
    });

    builder.UseRemote(
        transport => transport.Endpoint = new Uri("https://example.invalid/ingest"),
        encryption =>
        {
            encryption.KeyId = "2026-server";
            encryption.ServerPublicKeyBase64 = serverPublicKeyBase64;
        },
        otp => otp.SecretBase32 = sharedSecretBase32);

    builder.UseLocalFile(file => file.Directory = "telemetry-local");
});
```

```csharp
public sealed class Worker(ITelemetryClient telemetry)
{
    public void DoWork()
    {
        telemetry.TrackUsage("worker.start");
        try
        {
            Execute();
        }
        catch (Exception ex)
        {
            telemetry.TrackException(ex);
        }
    }
}
```

## サーバー側の鍵生成

```csharp
var keyPair = HybridKeyPair.Generate();
var serverPublicKeyBase64 = Convert.ToBase64String(keyPair.PublicKey);
```

受信側では `HybridPayloadOpener` でペイロードを復号し、`IOneTimePasswordProvider.Validate` でワンタイムパスワードを検証できます。

## ライセンス

MIT
