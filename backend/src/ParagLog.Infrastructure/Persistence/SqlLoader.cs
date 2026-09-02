using System.Reflection;

namespace ParagLog.Infrastructure.Persistence;

internal static class SqlLoader
{
    public static string Load(string name)
    {
        var assembly = typeof(SqlLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream($"ParagLog.Infrastructure.Persistence.Sql.{name}.sql")
            ?? throw new InvalidOperationException($"Embedded SQL resource '{name}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
