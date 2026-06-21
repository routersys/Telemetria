namespace Telemetria.Security;

/// <summary>
/// RFC 4648 に基づく Base32 のエンコードおよびデコードを提供します。
/// </summary>
public static class Base32
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>バイト列を Base32 文字列へエンコードします。</summary>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder((data.Length + 4) / 5 * 8);
        int buffer = 0;
        int bitsLeft = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                builder.Append(Alphabet[(buffer >> bitsLeft) & 0x1F]);
            }
        }

        if (bitsLeft > 0)
        {
            builder.Append(Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
        }

        while (builder.Length % 8 != 0)
        {
            builder.Append('=');
        }

        return builder.ToString();
    }

    /// <summary>Base32 文字列をバイト列へデコードします。</summary>
    public static byte[] Decode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmed = value.TrimEnd('=').Replace(" ", string.Empty).ToUpperInvariant();
        if (trimmed.Length == 0)
        {
            return [];
        }

        var output = new List<byte>(trimmed.Length * 5 / 8);
        int buffer = 0;
        int bitsLeft = 0;

        foreach (var c in trimmed)
        {
            var index = Alphabet.IndexOf(c);
            if (index < 0)
            {
                throw new FormatException($"Base32 として不正な文字が含まれています: '{c}'。");
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return [.. output];
    }
}
