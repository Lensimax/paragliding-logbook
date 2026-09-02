using System.Reflection;

namespace ParagLog.Core.Common.Validation;

public static class PasswordRules
{
    public const int MinLength = 8;

    private static readonly Lazy<HashSet<string>> CommonPasswords = new(LoadCommonPasswords);

    public static bool HasMinLength(string password) => password.Length >= MinLength;

    public static bool IsCommonPassword(string password) => CommonPasswords.Value.Contains(password);

    private static HashSet<string> LoadCommonPasswords()
    {
        var assembly = typeof(PasswordRules).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            "ParagLog.Core.Common.Validation.common-passwords.txt")
            ?? throw new InvalidOperationException("Embedded common-passwords.txt resource not found.");
        using var reader = new StreamReader(stream);

        var set = new HashSet<string>(StringComparer.Ordinal);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length > 0)
                set.Add(line);
        }

        return set;
    }
}
