using Microsoft.Extensions.Options;
using Telemetria.Anonymization;
using Xunit;

namespace Telemetria.Tests.Anonymization;

public sealed class DefaultAnonymizerTests
{
    private static DefaultAnonymizer Create(Action<AnonymizationOptions>? configure = null)
    {
        var options = new AnonymizationOptions();
        configure?.Invoke(options);
        return new DefaultAnonymizer(Options.Create(options));
    }

    private static TelemetrySignal SignalWith(Dictionary<string, string> properties)
        => new()
        {
            Category = SignalCategory.Usage,
            Name = "test",
            Properties = properties
        };

    [Fact]
    public void Anonymize_RedactsConfiguredKeys()
    {
        var anonymizer = Create(o => o.RedactedKeys.Add("userId"));
        var result = anonymizer.Anonymize(SignalWith(new Dictionary<string, string> { ["userId"] = "alice" }));

        Assert.StartsWith("sha256:", result.Properties["userId"]);
        Assert.DoesNotContain("alice", result.Properties["userId"]);
    }

    [Fact]
    public void Anonymize_RedactedKeyIsDeterministic()
    {
        var anonymizer = Create(o => o.RedactedKeys.Add("userId"));
        var first = anonymizer.Anonymize(SignalWith(new Dictionary<string, string> { ["userId"] = "alice" }));
        var second = anonymizer.Anonymize(SignalWith(new Dictionary<string, string> { ["userId"] = "alice" }));

        Assert.Equal(first.Properties["userId"], second.Properties["userId"]);
    }

    [Fact]
    public void Anonymize_MasksEmailAddresses()
    {
        var anonymizer = Create();
        var result = anonymizer.Anonymize(SignalWith(new Dictionary<string, string> { ["note"] = "連絡先は alice@example.com です" }));

        Assert.Contains("[email]", result.Properties["note"]);
        Assert.DoesNotContain("alice@example.com", result.Properties["note"]);
    }

    [Fact]
    public void Anonymize_MasksIpAddresses()
    {
        var anonymizer = Create();
        var result = anonymizer.Anonymize(SignalWith(new Dictionary<string, string> { ["host"] = "192.168.10.5 へ接続" }));

        Assert.Contains("[ip]", result.Properties["host"]);
        Assert.DoesNotContain("192.168.10.5", result.Properties["host"]);
    }

    [Fact]
    public void Anonymize_ScrubsWindowsUserPaths()
    {
        var anonymizer = Create();
        var result = anonymizer.Anonymize(SignalWith(new Dictionary<string, string> { ["path"] = @"C:\Users\johndoe\AppData\app.log" }));

        Assert.DoesNotContain("johndoe", result.Properties["path"]);
        Assert.Contains("[user]", result.Properties["path"]);
    }

    [Fact]
    public void Anonymize_LeavesBenignValuesUntouched()
    {
        var anonymizer = Create();
        var result = anonymizer.Anonymize(SignalWith(new Dictionary<string, string> { ["op"] = "save-document" }));

        Assert.Equal("save-document", result.Properties["op"]);
    }

    [Fact]
    public void Anonymize_ScrubsExceptionMessageAndStackTrace()
    {
        var anonymizer = Create();
        var signal = new TelemetrySignal
        {
            Category = SignalCategory.Exception,
            Name = "boom",
            Exception = new ExceptionSnapshot
            {
                Type = "System.Exception",
                Message = "failed for bob@example.com",
                StackTrace = @"at App in C:\Users\bob\src\App.cs",
                Inner = new ExceptionSnapshot { Type = "System.IO.IOException", Message = "10.0.0.1 unreachable" }
            }
        };

        var result = anonymizer.Anonymize(signal);

        Assert.Contains("[email]", result.Exception!.Message);
        Assert.Contains("[user]", result.Exception.StackTrace!);
        Assert.Contains("[ip]", result.Exception.Inner!.Message);
    }

    [Fact]
    public void Anonymize_CanDisableMasking()
    {
        var anonymizer = Create(o =>
        {
            o.RedactEmails = false;
            o.RedactIpAddresses = false;
            o.ScrubFilePaths = false;
        });

        var result = anonymizer.Anonymize(SignalWith(new Dictionary<string, string> { ["note"] = "alice@example.com" }));
        Assert.Equal("alice@example.com", result.Properties["note"]);
    }
}
