using System.Text.RegularExpressions;

namespace ParagLog.Core.Common.Validation;

public static partial class UsernameRules
{
    public static bool IsValid(string username) =>
        !string.IsNullOrEmpty(username) && Pattern().IsMatch(username);

    [GeneratedRegex("^[a-zA-Z0-9_-]{3,20}$")]
    private static partial Regex Pattern();
}
