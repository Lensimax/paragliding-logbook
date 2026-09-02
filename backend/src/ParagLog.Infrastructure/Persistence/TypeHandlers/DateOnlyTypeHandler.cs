using System.Data;
using Dapper;

namespace ParagLog.Infrastructure.Persistence.TypeHandlers;

/// <summary>
/// Dapper has no built-in binding for <see cref="DateOnly"/> parameters, even though Npgsql
/// natively supports DateOnly &lt;-&gt; the Postgres <c>date</c> type once a value reaches it.
/// </summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly dateOnly => dateOnly,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => DateOnly.Parse((string)value),
    };
}
