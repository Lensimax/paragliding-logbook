namespace ParagLog.Core.Common;

/// <summary>
/// Guards against names that are invalid directory names on Windows, since blob folder
/// names (derived from public IDs) must work identically on Windows and Linux.
/// </summary>
public static class PathSafety
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>True if <paramref name="name"/> (or its part before the first '.') is a Windows reserved device name.</summary>
    public static bool IsReservedName(string name)
    {
        var baseName = name.Split('.', 2)[0];
        return ReservedNames.Contains(baseName);
    }

    /// <summary>True if <paramref name="name"/> ends with a dot or a space, which Windows silently strips.</summary>
    public static bool HasUnsafeTrailingCharacters(string name) =>
        name.Length > 0 && (name[^1] == '.' || name[^1] == ' ');
}
