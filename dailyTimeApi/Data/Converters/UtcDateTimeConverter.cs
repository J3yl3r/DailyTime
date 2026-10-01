using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace dailyTimeApi.Data.Converters;

/// <summary>
/// Las columnas <c>datetime2</c> guardan instantes UTC, pero SQL Server no conserva el <see cref="DateTimeKind"/>:
/// sin este conversor EF los lee como <see cref="DateTimeKind.Unspecified"/>, System.Text.Json los serializa
/// sin "Z" y el navegador los interpreta como hora local. Al leer se marcan como UTC; al guardar, un valor
/// <see cref="DateTimeKind.Local"/> se pasa a UTC.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
