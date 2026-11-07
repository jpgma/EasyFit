using Npgsql;
using Wrexercise.Infrastructure.Errors;

namespace Wrexercise.Features.Grupos.Contracts;

/// <summary>Verificação de pertencimento ao grupo, usada por toda fatia com dados de um grupo.</summary>
internal static class GrupoAcesso
{
    /// <returns>O papel de quem ainda é membro; <c>null</c> para quem não é ou já saiu.</returns>
    public static async Task<string?> GetPapelAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await GetPapelAsync(connection, null, grupoId, usuarioId, cancellationToken);
    }

    public static async Task<string?> GetPapelAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""
            SELECT papel
            FROM membros_grupo
            WHERE grupo_id = $1 AND usuario_id = $2 AND removed_at IS NULL
            """, connection, transaction);
        command.Parameters.AddWithValue(grupoId);
        command.Parameters.AddWithValue(usuarioId);
        return (string?)await command.ExecuteScalarAsync(cancellationToken);
    }

    /// <summary>
    /// Serializa as alterações de um grupo até o fim da transação. Leia o papel em um comando
    /// seguinte: um único comando que trave e leia pode enxergar dados anteriores à espera.
    /// Sempre trave o grupo antes dos convites, para evitar deadlock.
    /// </summary>
    public static async Task LockGrupoAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid grupoId,
        CancellationToken cancellationToken)
    {
        // NO KEY não bloqueia as verificações de chave estrangeira de quem só referencia o grupo.
        await using var command = new NpgsqlCommand(
            "SELECT 1 FROM grupos WHERE id = $1 FOR NO KEY UPDATE",
            connection,
            transaction);
        command.Parameters.AddWithValue(grupoId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // A mesma resposta para quem não é membro e para um grupo inexistente: a existência não é revelada.
    public static IResult GrupoNaoEncontrado() => ApiProblem.Create(
        StatusCodes.Status404NotFound,
        "GRUPO_NAO_ENCONTRADO",
        "Grupo não encontrado.");

    public static IResult AcessoNegado() => ApiProblem.Create(
        StatusCodes.Status403Forbidden,
        "ACESSO_NEGADO",
        "Você não tem permissão para fazer isso neste grupo.");

    public static IResult GrupoCheio() => ApiProblem.Create(
        StatusCodes.Status409Conflict,
        "GRUPO_CHEIO",
        "Este grupo atingiu o número máximo de membros.");
}
