using Npgsql;
using Wrexercise.Features.Grupos.Contracts;
using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Grupos;

internal sealed record MembrosResponse(IReadOnlyList<MembroResponse> Membros);

internal static class ListarMembros
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var papel = await GrupoAcesso.GetPapelAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);
        if (papel is null)
        {
            return GrupoAcesso.GrupoNaoEncontrado();
        }

        var membros = await ListAsync(dataSource, id, cancellationToken);
        return Results.Ok(new MembrosResponse(membros));
    }

    private static async Task<IReadOnlyList<MembroResponse>> ListAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT m.usuario_id, u.nome_exibicao, m.papel, m.joined_at
            FROM membros_grupo m
            JOIN usuarios u ON u.id = m.usuario_id
            WHERE m.grupo_id = $1 AND m.removed_at IS NULL
            ORDER BY
                CASE m.papel WHEN 'Proprietario' THEN 0 WHEN 'Administrador' THEN 1 ELSE 2 END,
                u.nome_exibicao,
                m.usuario_id
            """);
        command.Parameters.AddWithValue(grupoId);

        var membros = new List<MembroResponse>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            membros.Add(new MembroResponse(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return membros;
    }
}
