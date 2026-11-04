using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Json;
using Npgsql;

namespace EasyFit.Features.Convites;

internal enum ConviteCriacaoStatus
{
    Criado,
    GrupoNaoEncontrado,
    AcessoNegado,
    GrupoCheio,
}

internal readonly record struct ConviteCriacaoResult(ConviteCriacaoStatus Status, ConviteCriadoResponse? Convite = null);

internal static class CriarConvite
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var result = await ExecutarAsync(
            dataSource,
            id,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return result.Status switch
        {
            ConviteCriacaoStatus.Criado => Results.Json(
                result.Convite!,
                AppJsonContext.Default.ConviteCriadoResponse,
                statusCode: StatusCodes.Status201Created),
            ConviteCriacaoStatus.GrupoNaoEncontrado => GrupoAcesso.GrupoNaoEncontrado(),
            ConviteCriacaoStatus.AcessoNegado => GrupoAcesso.AcessoNegado(),
            ConviteCriacaoStatus.GrupoCheio => GrupoAcesso.GrupoCheio(),
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
    }

    private static async Task<ConviteCriacaoResult> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Com o grupo travado, quem acabou de ser removido não consegue deixar um convite novo para trás.
        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var papel = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, usuarioId, cancellationToken);
        if (papel is null)
        {
            return new ConviteCriacaoResult(ConviteCriacaoStatus.GrupoNaoEncontrado);
        }

        if (!ValidacaoConvite.CanGerenciarConvites(papel))
        {
            return new ConviteCriacaoResult(ConviteCriacaoStatus.AcessoNegado);
        }

        await using (var count = new NpgsqlCommand(
            "SELECT count(*) FROM membros_grupo WHERE grupo_id = $1 AND removed_at IS NULL",
            connection,
            transaction))
        {
            count.Parameters.AddWithValue(grupoId);
            if ((long)(await count.ExecuteScalarAsync(cancellationToken))! >= GrupoEntrada.MaxMembros)
            {
                return new ConviteCriacaoResult(ConviteCriacaoStatus.GrupoCheio);
            }
        }

        var token = OpaqueToken.Generate();
        var expiresAt = DateTimeOffset.UtcNow + ValidacaoConvite.Validade;

        await using var insert = new NpgsqlCommand("""
            INSERT INTO convites_grupo (grupo_id, criador_id, token_hash, expires_at)
            VALUES ($1, $2, $3, $4)
            RETURNING id
            """, connection, transaction);
        insert.Parameters.AddWithValue(grupoId);
        insert.Parameters.AddWithValue(usuarioId);
        insert.Parameters.AddWithValue(OpaqueToken.Hash(token));
        insert.Parameters.AddWithValue(expiresAt);
        var id = (Guid)(await insert.ExecuteScalarAsync(cancellationToken))!;

        await transaction.CommitAsync(cancellationToken);
        return new ConviteCriacaoResult(ConviteCriacaoStatus.Criado, new ConviteCriadoResponse(id, token, expiresAt));
    }
}
