using Npgsql;
using Wrexercise.Features.Grupos.Contracts;
using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Grupos;

internal static class ObterGrupo
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var grupo = await GetAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return grupo is null ? GrupoAcesso.GrupoNaoEncontrado() : Results.Ok(grupo);
    }

    /// <returns>O grupo, ou <c>null</c> se ele não existir ou o usuário não for membro.</returns>
    private static async Task<GrupoResponse?> GetAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT g.id, g.nome, g.fuso_horario, m.papel,
                (SELECT count(*)::int FROM membros_grupo t WHERE t.grupo_id = g.id AND t.removed_at IS NULL)
            FROM grupos g
            JOIN membros_grupo m ON m.grupo_id = g.id
            WHERE g.id = $1 AND m.usuario_id = $2 AND m.removed_at IS NULL
            """);
        command.Parameters.AddWithValue(grupoId);
        command.Parameters.AddWithValue(usuarioId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new GrupoResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                GrupoEntrada.MaxMembros)
            : null;
    }
}
