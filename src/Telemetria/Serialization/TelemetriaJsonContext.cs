using System.Text.Json.Serialization;

namespace Telemetria.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SignalBatch))]
[JsonSerializable(typeof(TelemetrySignal))]
[JsonSerializable(typeof(ExceptionSnapshot))]
[JsonSerializable(typeof(ProtectedEnvelope))]
[JsonSerializable(typeof(ProtectedPayload))]
internal sealed partial class TelemetriaJsonContext : JsonSerializerContext;
