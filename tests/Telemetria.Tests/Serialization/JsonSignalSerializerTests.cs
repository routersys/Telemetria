using System.Text;
using Telemetria.Serialization;
using Xunit;

namespace Telemetria.Tests.Serialization;

public sealed class JsonSignalSerializerTests
{
    private readonly JsonSignalSerializer _serializer = new();

    [Fact]
    public void SerializeThenDeserialize_RoundTripsAllFields()
    {
        var batch = new SignalBatch
        {
            CreatedAt = DateTimeOffset.UnixEpoch.AddSeconds(1000),
            Signals =
            [
                new TelemetrySignal
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Category = SignalCategory.Exception,
                    Name = "boom",
                    Severity = SignalSeverity.Critical,
                    Timestamp = DateTimeOffset.UnixEpoch.AddSeconds(500),
                    Properties = new Dictionary<string, string> { ["k"] = "v" },
                    Measurements = new Dictionary<string, double> { ["m"] = 1.5 },
                    Exception = new ExceptionSnapshot
                    {
                        Type = "System.Exception",
                        Message = "failed",
                        Inner = new ExceptionSnapshot { Type = "System.IO.IOException", Message = "inner" }
                    }
                }
            ]
        };

        var restored = _serializer.Deserialize(_serializer.Serialize(batch));

        Assert.Equal(batch.CreatedAt, restored.CreatedAt);
        var signal = Assert.Single(restored.Signals);
        Assert.Equal(SignalCategory.Exception, signal.Category);
        Assert.Equal(SignalSeverity.Critical, signal.Severity);
        Assert.Equal("v", signal.Properties["k"]);
        Assert.Equal(1.5, signal.Measurements["m"]);
        Assert.Equal("inner", signal.Exception!.Inner!.Message);
    }

    [Fact]
    public void Serialize_WritesEnumsAsStrings()
    {
        var batch = new SignalBatch
        {
            CreatedAt = DateTimeOffset.UnixEpoch,
            Signals = [new TelemetrySignal { Category = SignalCategory.Usage, Name = "x", Severity = SignalSeverity.Warning }]
        };

        var json = Encoding.UTF8.GetString(_serializer.Serialize(batch));

        Assert.Contains("Usage", json);
        Assert.Contains("Warning", json);
    }

    [Fact]
    public void Deserialize_InvalidData_Throws()
        => Assert.ThrowsAny<Exception>(() => _serializer.Deserialize(Encoding.UTF8.GetBytes("not-json")));
}
