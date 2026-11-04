using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Errors;
using Npgsql;

namespace EasyFit.Features.Convites.Contracts;

internal enum ConviteAceiteStatus
{
    Aceito,
    ConviteInvalido,
    JaMembro,
    GrupoCheio,
}

internal readonly record struct ConviteAceiteResult(ConviteAceiteStatus Status, Guid GrupoId = default);

internal static class ConviteAceite
{
    private const int MaxTokenLength = 128;

    /// <summary>Descarta sem ir ao banco o que nunca poderia ser um token.</summary>
    public static bool IsPlausibleToken(string? token) => token?.Length is > 0 and <= MaxTokenLength;

    /// <summary>
    /// Consome o convite e adiciona o membro na transação recebida. Se o resultado não for
    /// <see cref="ConviteAceiteStatus.Aceito"/>, quem chamou precisa desfazer a transação.
    /// </summary>
    public static async Task<ConviteAceiteResult> AceitarAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string? token,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        if (!IsPlausibleToken(token))
        {
            return new ConviteAceiteResult(ConviteAceiteStatus.ConviteInvalido);
        }

        var tokenHash = OpaqueToken.Hash(token!);

        Guid grupoId;
        await using (var find = new NpgsqlCommand("""
            SELECT grupo_id
            FROM convites_grupo
            WHERE token_hash = $1 AND used_at IS NULL AND revoked_at IS NULL AND expires_at > now()
            """, connection, transaction))
        {
            find.Parameters.AddWithValue(tokenHash);
            if (await find.ExecuteScalarAsync(cancellationToken) is not Guid found)
            {
                return new ConviteAceiteResult(ConviteAceiteStatus.ConviteInvalido);
            }

            grupoId = found;
        }

        // A entrada trava o grupo antes de o convite ser tocado: é a mesma ordem da remoção de
        // membros, que também cancela convites. Na ordem inversa, as duas poderiam entrar em deadlock.
        var entrada = await GrupoEntrada.AddMembroAsync(connection, transaction, grupoId, usuarioId, cancellationToken);
        switch (entrada)
        {
            case GrupoEntradaStatus.JaMembro:
                return new ConviteAceiteResult(ConviteAceiteStatus.JaMembro);
            case GrupoEntradaStatus.GrupoCheio:
                return new ConviteAceiteResult(ConviteAceiteStatus.GrupoCheio);
        }

        // Repete as condições: o convite pode ter sido usado ou cancelado durante a espera pela trava.
        await using var consume = new NpgsqlCommand("""
            UPDATE convites_grupo
            SET used_at = now(), usado_por_id = $2
            WHERE token_hash = $1 AND used_at IS NULL AND revoked_at IS NULL AND expires_at > now()
            """, connection, transaction);
        consume.Parameters.AddWithValue(tokenHash);
        consume.Parameters.AddWithValue(usuarioId);

        return await consume.ExecuteNonQueryAsync(cancellationToken) == 1
            ? new ConviteAceiteResult(ConviteAceiteStatus.Aceito, grupoId)
            : new ConviteAceiteResult(ConviteAceiteStatus.ConviteInvalido);
    }

    // A mesma resposta para convite inexistente, expirado, usado ou cancelado.
    public static IResult ConviteInvalido() => ApiProblem.Create(
        StatusCodes.Status404NotFound,
        "CONVITE_INVALIDO",
        "Este convite não é válido. Peça um novo link a quem convidou você.");
}
