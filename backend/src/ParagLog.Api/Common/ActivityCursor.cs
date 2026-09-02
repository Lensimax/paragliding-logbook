using System.Globalization;
using System.Text;

namespace ParagLog.Api.Common;

/// <summary>Opaque keyset-pagination cursor over (started_at DESC, id DESC).</summary>
public static class ActivityCursor
{
    public static string Encode(DateTimeOffset startedAt, Guid id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{startedAt:O}|{id}"));

    public static (DateTimeOffset StartedAt, Guid Id)? Decode(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
            return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|', 2);
            if (parts.Length != 2)
                return null;

            var startedAt = DateTimeOffset.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var id = Guid.Parse(parts[1]);
            return (startedAt, id);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            return null;
        }
    }
}
