using System.Globalization;
using System.Text;

namespace Wrexercise.Features.Atividades;

internal static class PaginacaoAtividade
{
    public static string? EncodeCursor(DateTimeOffset startedAt, Guid id)
    {
        var texto = $"{startedAt.UtcTicks}|{id:N}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(texto));
    }

    public static bool TryDecodeCursor(string? cursor, out DateTimeOffset startedAt, out Guid id)
    {
        startedAt = default;
        id = default;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            var texto = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var separador = texto.IndexOf('|', StringComparison.Ordinal);
            if (separador <= 0)
            {
                return false;
            }

            if (!long.TryParse(texto.AsSpan(0, separador), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            {
                return false;
            }

            startedAt = new DateTimeOffset(ticks, TimeSpan.Zero);
            return Guid.TryParseExact(texto.AsSpan(separador + 1), "N", out id);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
