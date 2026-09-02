using System.Data;
using System.Text;
using Dapper;

namespace ParagLog.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// Converts a C# enum (PascalCase) to/from a Postgres enum label (snake_case), e.g.
/// <c>GroundHandling</c> &lt;-&gt; <c>ground_handling</c>.
///
/// Npgsql's type resolver has no default CLR mapping for custom Postgres enum types (the same
/// issue hit with citext), so read queries must cast the column to text; write queries must cast
/// the bound text parameter back with <c>@Param::the_pg_enum</c>, since Postgres has no implicit
/// text-to-enum assignment cast for bound parameters.
///
/// Neither <see cref="SetValue"/> nor <see cref="Parse"/> - the SqlMapper.TypeHandler override
/// points - ever actually runs: Dapper special-cases any enum-typed parameter or POCO property to
/// go through plain DbType.Int32 / Enum.Parse before it consults a registered handler, for both
/// writes and reads. That went unnoticed for single-word labels ("flight", "wing"...), which
/// round-trip through Enum.Parse's case-insensitive name match by coincidence - multi-word labels
/// like "ground_handling" don't. <see cref="ToLabel"/> and <see cref="FromLabel"/> are the real
/// mechanism: repositories must read enum columns as plain text into a string row property and
/// call FromLabel explicitly, and convert enum values with ToLabel before binding them as a
/// parameter - never bind a C# enum type directly to a Dapper parameter or POCO property.
/// </summary>
public sealed class PgEnumTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum> where TEnum : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, TEnum value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = ToLabel(value);
    }

    public override TEnum Parse(object value) => FromLabel((string)value);

    public static string ToLabel(TEnum value)
    {
        var name = value.ToString();
        var builder = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                builder.Append('_');
            builder.Append(char.ToLowerInvariant(name[i]));
        }
        return builder.ToString();
    }

    public static TEnum FromLabel(string label)
    {
        var pascal = string.Concat(label.Split('_').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        return Enum.Parse<TEnum>(pascal);
    }
}
