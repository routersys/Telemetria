using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Telemetria.Security;

/// <summary>
/// RFC 6238 に基づく時刻基準ワンタイムパスワードの生成および検証を行います。
/// </summary>
public sealed class TotpProvider : IOneTimePasswordProvider
{
    private readonly byte[] _secret;
    private readonly int _digits;
    private readonly long _periodSeconds;
    private readonly OtpAlgorithm _algorithm;
    private readonly int _windowSteps;
    private readonly TimeProvider _timeProvider;

    /// <summary>指定したオプションと時刻源で初期化します。</summary>
    public TotpProvider(OneTimePasswordOptions options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _secret = ResolveSecret(options.SecretBase32);
        _digits = options.Digits is >= 6 and <= 8 ? options.Digits : throw new ArgumentOutOfRangeException(nameof(options), "桁数は 6 から 8 の範囲で指定してください。");
        _periodSeconds = (long)options.Period.TotalSeconds;
        if (_periodSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "有効期間は正の値で指定してください。");
        }

        _algorithm = options.Algorithm;
        _windowSteps = Math.Max(0, options.ValidationWindowSteps);
        _timeProvider = timeProvider;
    }

    /// <summary>テスト用途などのために生のシークレットで初期化します。</summary>
    public TotpProvider(byte[] secret, int digits, TimeSpan period, OtpAlgorithm algorithm, int windowSteps, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _secret = secret;
        _digits = digits;
        _periodSeconds = (long)period.TotalSeconds;
        _algorithm = algorithm;
        _windowSteps = Math.Max(0, windowSteps);
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public string Generate() => GenerateAt(_timeProvider.GetUtcNow());

    /// <inheritdoc />
    public string GenerateAt(DateTimeOffset timestamp) => Compute(CounterFor(timestamp));

    /// <inheritdoc />
    public bool Validate(string code) => Validate(code, _timeProvider.GetUtcNow());

    /// <inheritdoc />
    public bool Validate(string code, DateTimeOffset timestamp)
    {
        if (string.IsNullOrEmpty(code))
        {
            return false;
        }

        var counter = CounterFor(timestamp);
        for (var step = -_windowSteps; step <= _windowSteps; step++)
        {
            var candidate = Compute(counter + step);
            if (CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.ASCII.GetBytes(candidate),
                    System.Text.Encoding.ASCII.GetBytes(code)))
            {
                return true;
            }
        }

        return false;
    }

    private long CounterFor(DateTimeOffset timestamp) => timestamp.ToUnixTimeSeconds() / _periodSeconds;

    private string Compute(long counter)
    {
        Span<byte> message = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(message, counter);

        Span<byte> hash = stackalloc byte[64];
        var written = _algorithm switch
        {
            OtpAlgorithm.Sha1 => HMACSHA1.HashData(_secret, message, hash),
            OtpAlgorithm.Sha256 => HMACSHA256.HashData(_secret, message, hash),
            OtpAlgorithm.Sha512 => HMACSHA512.HashData(_secret, message, hash),
            _ => throw new ArgumentOutOfRangeException(nameof(counter))
        };

        var mac = hash[..written];
        var offset = mac[^1] & 0x0F;
        var binary = ((mac[offset] & 0x7F) << 24)
                     | ((mac[offset + 1] & 0xFF) << 16)
                     | ((mac[offset + 2] & 0xFF) << 8)
                     | (mac[offset + 3] & 0xFF);

        var modulo = (int)Math.Pow(10, _digits);
        var otp = binary % modulo;
        return otp.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(_digits, '0');
    }

    private static byte[] ResolveSecret(string? secretBase32)
    {
        if (!string.IsNullOrWhiteSpace(secretBase32))
        {
            return Base32.Decode(secretBase32);
        }

        var generated = new byte[20];
        RandomNumberGenerator.Fill(generated);
        return generated;
    }
}
