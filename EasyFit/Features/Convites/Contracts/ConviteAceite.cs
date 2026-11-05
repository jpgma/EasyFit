using EasyFit.Features.Grupos.Contracts;
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
    /// Adiciona o membro na transação recebida, sem gastar o link. Se o resultado não for
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

        Guid grupoId;
        await using (var find = new NpgsqlCommand("""
            SELECT grupo_id
            FROM convites_grupo
            WHERE token = $1 AND revoked_at IS NULL AND expires_at > now()
            """, connection, transaction))
        {
            find.Parameters.AddWithValue(token!);
            if (await find.ExecuteScalarAsync(cancellationToken) is not Guid found)
            {
                return new ConviteAceiteResult(ConviteAceiteStatus.ConviteInvalido);
            }

            grupoId = found;
        }

        // A entrada trava o grupo antes de o convite ser relido: é a mesma ordem da remoção de
        // membros, que também cancela convites.
        var entrada = await GrupoEntrada.AddMembroAsync(connection, transaction, grupoId, usuarioId, cancellationToken);
        switch (entrada)
        {
            case GrupoEntradaStatus.JaMembro:
                return new ConviteAceiteResult(ConviteAceiteStatus.JaMembro);
            case GrupoEntradaStatus.GrupoCheio:
                return new ConviteAceiteResult(ConviteAceiteStatus.GrupoCheio);
        }

        // Repete as condições: o link pode ter sido cancelado ou substituído durante a espera pela trava.
        await using var ativo = new NpgsqlCommand("""
            SELECT 1
            FROM convites_grupo
            WHERE token = $1 AND revoked_at IS NULL AND expires_at > now()
            """, connection, transaction);
        ativo.Parameters.AddWithValue(token!);

        return await ativo.ExecuteScalarAsync(cancellationToken) is not null
            ? new ConviteAceiteResult(ConviteAceiteStatus.Aceito, grupoId)
            : new ConviteAceiteResult(ConviteAceiteStatus.ConviteInvalido);
    }

    // A mesma resposta para convite inexistente, expirado ou cancelado.
    public static IResult ConviteInvalido() => ApiProblem.Create(
        StatusCodes.Status404NotFound,
        "CONVITE_INVALIDO",
        "Este convite não é válido. Peça um novo link a quem convidou você.");
}
