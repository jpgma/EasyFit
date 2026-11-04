using EasyFit.Features.Grupos.Contracts;
using Npgsql;

namespace EasyFit.Features.Grupos;

internal enum GrupoOperacaoStatus
{
    Concluida,
    GrupoNaoEncontrado,
    AcessoNegado,
    MembroNaoEncontrado,
    ProprietarioNaoPodeSair,
    DadosInvalidos,
}

/// <summary>
/// As permissões são verificadas depois de travar o grupo: duas alterações simultâneas
/// não podem decidir com base em papéis que a outra acabou de mudar.
/// </summary>
internal static class GruposCommands
{
    public static async Task<GrupoResponse> CreateAsync(
        NpgsqlDataSource dataSource,
        string nome,
        string fusoHorario,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        Guid grupoId;
        await using (var grupo = new NpgsqlCommand("""
            INSERT INTO grupos (nome, criador_id, fuso_horario)
            VALUES ($1, $2, $3)
            RETURNING id
            """, connection, transaction))
        {
            grupo.Parameters.AddWithValue(nome);
            grupo.Parameters.AddWithValue(usuarioId);
            grupo.Parameters.AddWithValue(fusoHorario);
            grupoId = (Guid)(await grupo.ExecuteScalarAsync(cancellationToken))!;
        }

        await using (var membro = new NpgsqlCommand(
            "INSERT INTO membros_grupo (grupo_id, usuario_id, papel) VALUES ($1, $2, $3)",
            connection,
            transaction))
        {
            membro.Parameters.AddWithValue(grupoId);
            membro.Parameters.AddWithValue(usuarioId);
            membro.Parameters.AddWithValue(PapelMembro.Proprietario);
            await membro.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new GrupoResponse(grupoId, nome, fusoHorario, PapelMembro.Proprietario, 1, GrupoEntrada.MaxMembros);
    }

    public static async Task<GrupoOperacaoStatus> AlterarPapelAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid atorId,
        Guid alvoId,
        string papel,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var ator = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, atorId, cancellationToken);
        if (ator is null)
        {
            return GrupoOperacaoStatus.GrupoNaoEncontrado;
        }

        var alvo = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, alvoId, cancellationToken);
        if (alvo is null)
        {
            return GrupoOperacaoStatus.MembroNaoEncontrado;
        }

        if (!GruposRules.CanAlterarPapel(ator, alvo))
        {
            return GrupoOperacaoStatus.AcessoNegado;
        }

        await SetPapelAsync(connection, transaction, grupoId, alvoId, papel, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return GrupoOperacaoStatus.Concluida;
    }

    public static async Task<GrupoOperacaoStatus> RemoverMembroAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid atorId,
        Guid alvoId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var ator = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, atorId, cancellationToken);
        if (ator is null)
        {
            return GrupoOperacaoStatus.GrupoNaoEncontrado;
        }

        var alvo = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, alvoId, cancellationToken);
        if (alvo is null)
        {
            return GrupoOperacaoStatus.MembroNaoEncontrado;
        }

        if (!GruposRules.CanRemoverMembro(ator, alvo))
        {
            return GrupoOperacaoStatus.AcessoNegado;
        }

        await DesligarMembroAsync(connection, transaction, grupoId, alvoId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return GrupoOperacaoStatus.Concluida;
    }

    public static async Task<GrupoOperacaoStatus> SairAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var papel = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, usuarioId, cancellationToken);
        if (papel is null)
        {
            return GrupoOperacaoStatus.GrupoNaoEncontrado;
        }

        if (!GruposRules.CanSair(papel))
        {
            return GrupoOperacaoStatus.ProprietarioNaoPodeSair;
        }

        await DesligarMembroAsync(connection, transaction, grupoId, usuarioId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return GrupoOperacaoStatus.Concluida;
    }

    public static async Task<GrupoOperacaoStatus> TransferirAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid atorId,
        Guid alvoId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var ator = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, atorId, cancellationToken);
        if (ator is null)
        {
            return GrupoOperacaoStatus.GrupoNaoEncontrado;
        }

        if (!GruposRules.CanTransferir(ator))
        {
            return GrupoOperacaoStatus.AcessoNegado;
        }

        if (alvoId == atorId)
        {
            return GrupoOperacaoStatus.DadosInvalidos;
        }

        var alvo = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, alvoId, cancellationToken);
        if (alvo is null)
        {
            return GrupoOperacaoStatus.MembroNaoEncontrado;
        }

        // Nesta ordem por causa do índice que garante um único proprietário ativo.
        await SetPapelAsync(connection, transaction, grupoId, atorId, PapelMembro.Administrador, cancellationToken);
        await SetPapelAsync(connection, transaction, grupoId, alvoId, PapelMembro.Proprietario, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return GrupoOperacaoStatus.Concluida;
    }

    private static async Task SetPapelAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid grupoId,
        Guid usuarioId,
        string papel,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "UPDATE membros_grupo SET papel = $3 WHERE grupo_id = $1 AND usuario_id = $2",
            connection,
            transaction);
        command.Parameters.AddWithValue(grupoId);
        command.Parameters.AddWithValue(usuarioId);
        command.Parameters.AddWithValue(papel);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Encerra o vínculo e cancela os convites pendentes de quem saiu, que não responde mais pelo grupo.</summary>
    private static async Task DesligarMembroAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using (var membro = new NpgsqlCommand(
            "UPDATE membros_grupo SET removed_at = now() WHERE grupo_id = $1 AND usuario_id = $2",
            connection,
            transaction))
        {
            membro.Parameters.AddWithValue(grupoId);
            membro.Parameters.AddWithValue(usuarioId);
            await membro.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var convites = new NpgsqlCommand("""
            UPDATE convites_grupo
            SET revoked_at = now()
            WHERE grupo_id = $1 AND criador_id = $2 AND used_at IS NULL AND revoked_at IS NULL
            """, connection, transaction);
        convites.Parameters.AddWithValue(grupoId);
        convites.Parameters.AddWithValue(usuarioId);
        await convites.ExecuteNonQueryAsync(cancellationToken);
    }
}
