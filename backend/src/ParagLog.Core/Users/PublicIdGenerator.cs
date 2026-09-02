using System.Security.Cryptography;
using System.Text;
using ParagLog.Core.Common;

namespace ParagLog.Core.Users;

/// <summary>
/// Derives the immutable public ID used as the blob folder name and in URLs.
///
/// public_id = slug(lowercase(username)) + "-" + base32(sha256(username + created_at_ticks + server_salt))[0..5]
///
/// Must be a valid directory name on both Windows and Linux: lowercase only, no Windows
/// reserved device names, no trailing dots or spaces.
/// </summary>
public static class PublicIdGenerator
{
    private const int SuffixLength = 5;
    private const string Base32Alphabet = "abcdefghijklmnopqrstuvwxyz234567";

    public static string Generate(string username, DateTimeOffset createdAt, string serverSalt)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        ArgumentException.ThrowIfNullOrEmpty(serverSalt);

        var slug = Slugify(username);
        if (slug.Length == 0 || PathSafety.IsReservedName(slug))
            slug = $"u-{slug}".Trim('-');

        var suffix = ComputeSuffix(username, createdAt, serverSalt);
        var publicId = $"{slug}-{suffix}";

        if (PathSafety.IsReservedName(publicId) || PathSafety.HasUnsafeTrailingCharacters(publicId))
            throw new InvalidOperationException($"Generated public ID '{publicId}' is not a safe directory name.");

        return publicId;
    }

    private static string Slugify(string username)
    {
        var lowered = username.ToLowerInvariant();
        var builder = new StringBuilder(lowered.Length);

        foreach (var c in lowered)
            builder.Append(c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-');

        return CollapseAndTrimHyphens(builder.ToString());
    }

    private static string CollapseAndTrimHyphens(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasHyphen = false;

        foreach (var c in value)
        {
            if (c == '-')
            {
                if (!previousWasHyphen)
                    builder.Append('-');
                previousWasHyphen = true;
            }
            else
            {
                builder.Append(c);
                previousWasHyphen = false;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string ComputeSuffix(string username, DateTimeOffset createdAt, string serverSalt)
    {
        var input = $"{username}{createdAt.UtcTicks}{serverSalt}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Base32Encode(hash)[..SuffixLength];
    }

    private static string Base32Encode(byte[] data)
    {
        var result = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bitsInBuffer = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5)
            {
                bitsInBuffer -= 5;
                result.Append(Base32Alphabet[(buffer >> bitsInBuffer) & 0x1F]);
            }
        }

        if (bitsInBuffer > 0)
            result.Append(Base32Alphabet[(buffer << (5 - bitsInBuffer)) & 0x1F]);

        return result.ToString();
    }
}
