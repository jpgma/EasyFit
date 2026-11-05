using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Errors;
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

internal static class MembrosGrupo
{
    public static async Task SetPapelAsync(
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

    /// <summary>
    /// Encerra o vínculo e cancela o link criado por quem saiu, que não responde mais pelo grupo.
    /// Numa remoção, cancela o link de qualquer criador: quem foi removido pode tê-lo guardado.
    /// </summary>
    public static async Task DesligarAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid grupoId,
        Guid usuarioId,
        bool removido,
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
            WHERE grupo_id = $1 AND revoked_at IS NULL AND ($3 OR criador_id = $2)
            """, connection, transaction);
        convites.Parameters.AddWithValue(grupoId);
        convites.Parameters.AddWithValue(usuarioId);
        convites.Parameters.AddWithValue(removido);
        await convites.ExecuteNonQueryAsync(cancellationToken);
    }

    public static IResult ToResult(GrupoOperacaoStatus status) => status switch
    {
        GrupoOperacaoStatus.Concluida => Results.NoContent(),
        GrupoOperacaoStatus.GrupoNaoEncontrado => GrupoAcesso.GrupoNaoEncontrado(),
        GrupoOperacaoStatus.AcessoNegado => GrupoAcesso.AcessoNegado(),
        GrupoOperacaoStatus.MembroNaoEncontrado => ApiProblem.Create(
            StatusCodes.Status404NotFound,
            "MEMBRO_NAO_ENCONTRADO",
            "Esta pessoa não faz parte do grupo."),
        GrupoOperacaoStatus.ProprietarioNaoPodeSair => ApiProblem.Create(
            StatusCodes.Status409Conflict,
            "PROPRIETARIO_NAO_PODE_SAIR",
            "Transfira a propriedade do grupo para outro membro antes de sair."),
        GrupoOperacaoStatus.DadosInvalidos => InvalidData(),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static IResult InvalidData() => ApiProblem.Create(
        StatusCodes.Status400BadRequest,
        "DADOS_INVALIDOS",
        "Verifique os dados informados.");
}
