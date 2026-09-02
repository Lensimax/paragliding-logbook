using System.Data;
using System.Text;
using Dapper;

namespace ParagLog.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// Maps a C# enum (PascalCase) to/from a Postgres enum label (snake_case), e.g.
/// <c>GroundHandling</c> &lt;-&gt; <c>ground_handling</c>.
///
/// Npgsql's type resolver has no default CLR mapping for custom Postgres enum types (the same
/// issue hit with citext), so read queries must cast the column to text; write queries must cast
/// the bound text parameter back with <c>@Param::the_pg_enum</c>, since Postgres has no implicit
/// text-to-enum assignment cast for bound parameters.
///
/// This handler covers reads (<see cref="Parse"/>, used by Dapper's row deserializer). It does
/// NOT cover writes: Dapper's parameter binder special-cases enums to DbType.Int32 before it ever
/// consults a registered type handler, so <see cref="SetValue"/> is unreachable in practice.
/// Repositories must convert enum values to their label with <see cref="ToLabel"/> themselves
/// before adding them to a parameters object.
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
