using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Telemetria.Anonymization;

/// <summary>
/// 既定の匿名化処理です。指定されたキーの値の秘匿、メールアドレスや IP アドレスのマスク、
/// ファイルパスからのユーザー固有部分の除去を行います。
/// </summary>
public sealed partial class DefaultAnonymizer : IAnonymizer
{
    private readonly AnonymizationOptions _options;

    /// <summary>オプションで初期化します。</summary>
    public DefaultAnonymizer(IOptions<AnonymizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public TelemetrySignal Anonymize(TelemetrySignal signal)
    {
        var properties = AnonymizeProperties(signal.Properties);
        var exception = signal.Exception is null ? null : AnonymizeException(signal.Exception);
        return signal with { Properties = properties, Exception = exception };
    }

    private IReadOnlyDictionary<string, string> AnonymizeProperties(IReadOnlyDictionary<string, string> source)
    {
        if (source.Count == 0)
        {
            return source;
        }

        var result = new Dictionary<string, string>(source.Count, StringComparer.Ordinal);
        foreach (var (key, value) in source)
        {
            result[key] = _options.RedactedKeys.Contains(key) ? Hash(value) : Scrub(value);
        }

        return result;
    }

    private ExceptionSnapshot AnonymizeException(ExceptionSnapshot snapshot) => snapshot with
    {
        Message = Scrub(snapshot.Message),
        StackTrace = snapshot.StackTrace is null ? null : Scrub(snapshot.StackTrace),
        Inner = snapshot.Inner is null ? null : AnonymizeException(snapshot.Inner),
        Aggregated = [.. snapshot.Aggregated.Select(AnonymizeException)]
    };

    private string Scrub(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = value;
        if (_options.RedactEmails)
        {
            result = EmailRegex().Replace(result, "[email]");
        }

        if (_options.RedactIpAddresses)
        {
            result = IpV4Regex().Replace(result, "[ip]");
        }

        if (_options.ScrubFilePaths)
        {
            result = WindowsUserPathRegex().Replace(result, @"C:\Users\[user]");
            result = UnixHomePathRegex().Replace(result, "/home/[user]");
        }

        return result;
    }

    private string Hash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(_options.HashSalt + value);
        var hash = SHA256.HashData(bytes);
        return "sha256:" + Convert.ToHexStringLower(hash)[..16];
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.CultureInvariant)]
    private static partial Regex IpV4Regex();

    [GeneratedRegex(@"[A-Za-z]:\\Users\\[^\\/:*?""<>|\r\n]+", RegexOptions.CultureInvariant)]
    private static partial Regex WindowsUserPathRegex();

    [GeneratedRegex(@"/home/[^/\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex UnixHomePathRegex();
}
