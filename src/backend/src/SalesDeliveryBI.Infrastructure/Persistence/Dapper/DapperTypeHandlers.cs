using System.Data;
using System.Runtime.CompilerServices;
using global::Dapper;

namespace SalesDeliveryBI.Infrastructure.Persistence.Dapper;

/// <summary>Dapper has no built-in DateOnly support (unlike Npgsql, which handles it natively at the ADO.NET layer).</summary>
internal static class DapperTypeHandlers
{
#pragma warning disable CA2255 // deliberate: registers a Dapper type handler once per process, no other side effects
    [ModuleInitializer]
    internal static void Register()
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }
#pragma warning restore CA2255

    private sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value) => parameter.Value = value;

        public override DateOnly Parse(object value) => value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => throw new NotSupportedException($"Cannot convert {value.GetType()} to DateOnly."),
        };
    }
}
