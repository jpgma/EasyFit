using Npgsql;
using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Grupos;

internal sealed record GrupoResumoResponse(Guid Id, string Nome, string Papel, int TotalMembros);

internal sealed record GruposResponse(IReadOnlyList<GrupoResumoResponse> Grupos);

internal static class ListarGrupos
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var grupos = await ListByUsuarioAsync(
            dataSource,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return Results.Ok(new GruposResponse(grupos));
    }

    private static async Task<IReadOnlyList<GrupoResumoResponse>> ListByUsuarioAsync(
        NpgsqlDataSource dataSource,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT g.id, g.nome, m.papel,
                (SELECT count(*)::int FROM membros_grupo t WHERE t.grupo_id = g.id AND t.removed_at IS NULL)
            FROM membros_grupo m
            JOIN grupos g ON g.id = m.grupo_id
            WHERE m.usuario_id = $1 AND m.removed_at IS NULL
            ORDER BY m.joined_at, g.id
            """);
        command.Parameters.AddWithValue(usuarioId);

        var grupos = new List<GrupoResumoResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            grupos.Add(new GrupoResumoResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }

        return grupos;
    }
}
