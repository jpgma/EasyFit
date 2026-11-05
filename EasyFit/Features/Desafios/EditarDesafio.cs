using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Json;
using Npgsql;
using NpgsqlTypes;

namespace EasyFit.Features.Desafios;

internal sealed record EditarDesafioRequest(string? Nome, string? Descricao);

/// <summary>Só nome e descrição mudam: a meta e as datas valem para todos desde a criação.</summary>
internal static class EditarDesafio
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        EditarDesafioRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!ValidacaoDesafio.IsValidNome(request.Nome) || !ValidacaoDesafio.IsValidDescricao(request.Descricao))
        {
            return OperacaoDesafio.InvalidData();
        }

        var usuarioId = context.GetSessaoAtual().UsuarioId;
        var status = await ExecutarAsync(
            dataSource,
            id,
            usuarioId,
            ValidacaoDesafio.NormalizeNome(request.Nome!),
            ValidacaoDesafio.NormalizeDescricao(request.Descricao),
            cancellationToken);
        if (status != DesafioOperacaoStatus.Concluida)
        {
            return OperacaoDesafio.ToResult(status);
        }

        var desafio = await ConsultasDesafio.GetAsync(dataSource, id, usuarioId, cancellationToken);
        return desafio is null
            ? OperacaoDesafio.DesafioNaoEncontrado()
            : Results.Json(desafio, AppJsonContext.Default.DesafioResponse);
    }

    private static async Task<DesafioOperacaoStatus> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid desafioId,
        Guid usuarioId,
        string nome,
        string? descricao,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var encontrado = await ConsultasDesafio.GetEstadoAsync(connection, transaction, desafioId, cancellationToken);
        if (encontrado is null)
        {
            return DesafioOperacaoStatus.DesafioNaoEncontrado;
        }

        await GrupoAcesso.LockGrupoAsync(connection, transaction, encontrado.GrupoId, cancellationToken);
        var papel = await GrupoAcesso.GetPapelAsync(connection, transaction, encontrado.GrupoId, usuarioId, cancellationToken);
        if (papel is null)
        {
            return DesafioOperacaoStatus.DesafioNaoEncontrado;
        }

        if (!ValidacaoDesafio.CanGerenciar(papel))
        {
            return DesafioOperacaoStatus.AcessoNegado;
        }

        var estado = await ConsultasDesafio.GetEstadoAsync(connection, transaction, desafioId, cancellationToken);
        if (ConsultasDesafio.GetBloqueio(estado!.Situacao) is { } bloqueio)
        {
            return bloqueio;
        }

        await using var update = new NpgsqlCommand(
            "UPDATE desafios SET nome = $2, descricao = $3 WHERE id = $1",
            connection,
            transaction);
        update.Parameters.AddWithValue(desafioId);
        update.Parameters.AddWithValue(nome);
        update.Parameters.AddWithValue(NpgsqlDbType.Text, (object?)descricao ?? DBNull.Value);
        await update.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return DesafioOperacaoStatus.Concluida;
    }
}
