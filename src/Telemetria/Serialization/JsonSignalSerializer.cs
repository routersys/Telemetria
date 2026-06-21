using System.Text.Json;

namespace Telemetria.Serialization;

/// <summary>
/// System.Text.Json を用いてバッチの直列化と復元を行います。
/// </summary>
public sealed class JsonSignalSerializer : ISignalSerializer
{
    /// <inheritdoc />
    public byte[] Serialize(SignalBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        return JsonSerializer.SerializeToUtf8Bytes(batch, TelemetriaJsonContext.Default.SignalBatch);
    }

    /// <inheritdoc />
    public SignalBatch Deserialize(ReadOnlySpan<byte> data)
    {
        var result = JsonSerializer.Deserialize(data, TelemetriaJsonContext.Default.SignalBatch);
        return result ?? throw new FormatException("バッチを復元できませんでした。");
    }
}
