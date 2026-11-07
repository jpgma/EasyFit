using Npgsql;

namespace Wrexercise.Infrastructure.Database;

internal static class TimestampLeitura
{
    public static DateTimeOffset FromReader(NpgsqlDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => throw new InvalidCastException($"Coluna {ordinal} não é um timestamp com fuso."),
        };
    }

    public static DateTimeOffset FromScalar(object value) =>
        value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => throw new InvalidCastException("Valor não é um timestamp com fuso."),
        };
}
