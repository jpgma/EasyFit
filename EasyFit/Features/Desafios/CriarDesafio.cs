using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Authentication;
using EasyFit.Infrastructure.Json;
using Npgsql;
using NpgsqlTypes;

namespace EasyFit.Features.Desafios;

// Os campos são anuláveis porque o JSON recebido pode omiti-los; ValidacaoDesafio valida antes de usar.
internal sealed record CriarDesafioRequest(
    string? Nome,
    string? Descricao,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    int? DuracaoMinimaSegundos,
    int? SessoesPorSemana);

internal sealed record NovoDesafio(
    string Nome,
    string? Descricao,
    DateOnly DataInicio,
    DateOnly DataFim,
    int DuracaoMinimaSegundos,
    int SessoesPorSemana);

/// <summary>Quem altera um desafio trava o grupo antes de verificar o papel, como em toda operação do grupo.</summary>
internal static class CriarDesafio
{
    public static async Task<IResult> HandleAsync(
        Guid grupoId,
        CriarDesafioRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!ValidacaoDesafio.IsValidNome(request.Nome)
            || !ValidacaoDesafio.IsValidDescricao(request.Descricao)
            || !ValidacaoDesafio.IsValidDuracaoMinima(request.DuracaoMinimaSegundos)
            || !ValidacaoDesafio.IsValidSessoesPorSemana(request.SessoesPorSemana)
            || request.DataInicio is not { } dataInicio
            || request.DataFim is not { } dataFim)
        {
            return OperacaoDesafio.InvalidData();
        }

        var usuarioId = context.GetSessaoAtual().UsuarioId;
        var (status, desafioId) = await ExecutarAsync(
            dataSource,
            grupoId,
            usuarioId,
            new NovoDesafio(
                ValidacaoDesafio.NormalizeNome(request.Nome!),
                ValidacaoDesafio.NormalizeDescricao(request.Descricao),
                dataInicio,
                dataFim,
                request.DuracaoMinimaSegundos!.Value,
                request.SessoesPorSemana!.Value),
            cancellationToken);
        if (status != DesafioOperacaoStatus.Concluida)
        {
            return OperacaoDesafio.ToResult(status);
        }

        var desafio = await ConsultasDesafio.GetAsync(dataSource, desafioId, usuarioId, cancellationToken);
        return desafio is null
            ? OperacaoDesafio.DesafioNaoEncontrado()
            : Results.Json(desafio, AppJsonContext.Default.DesafioResponse, statusCode: StatusCodes.Status201Created);
    }

    /// <summary>A trava do grupo serializa a verificação de sobreposição entre criações simultâneas.</summary>
    private static async Task<(DesafioOperacaoStatus Status, Guid Id)> ExecutarAsync(
        NpgsqlDataSource dataSource,
        Guid grupoId,
        Guid usuarioId,
        NovoDesafio novo,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await GrupoAcesso.LockGrupoAsync(connection, transaction, grupoId, cancellationToken);
        var papel = await GrupoAcesso.GetPapelAsync(connection, transaction, grupoId, usuarioId, cancellationToken);
        if (papel is null)
        {
            return (DesafioOperacaoStatus.GrupoNaoEncontrado, Guid.Empty);
        }

        if (!ValidacaoDesafio.CanGerenciar(papel))
        {
            return (DesafioOperacaoStatus.AcessoNegado, Guid.Empty);
        }

        DateOnly hoje;
        await using (var select = new NpgsqlCommand(
            "SELECT (now() AT TIME ZONE fuso_horario)::date FROM grupos WHERE id = $1",
            connection,
            transaction))
        {
            select.Parameters.AddWithValue(grupoId);
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            hoje = reader.GetFieldValue<DateOnly>(0);
        }

        if (!ValidacaoDesafio.IsValidPeriodo(novo.DataInicio, novo.DataFim, hoje))
        {
            return (DesafioOperacaoStatus.DadosInvalidos, Guid.Empty);
        }

        await using (var sobreposto = new NpgsqlCommand("""
            SELECT 1 FROM desafios
            WHERE grupo_id = $1 AND cancelled_at IS NULL AND data_inicio <= $3 AND data_fim >= $2
            LIMIT 1
            """, connection, transaction))
        {
            sobreposto.Parameters.AddWithValue(grupoId);
            sobreposto.Parameters.AddWithValue(novo.DataInicio);
            sobreposto.Parameters.AddWithValue(novo.DataFim);
            if (await sobreposto.ExecuteScalarAsync(cancellationToken) is not null)
            {
                return (DesafioOperacaoStatus.DesafioSobreposto, Guid.Empty);
            }
        }

        Guid desafioId;
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO desafios (
                grupo_id, criador_id, nome, descricao, data_inicio, data_fim,
                duracao_minima_segundos, sessoes_por_semana)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
            RETURNING id
            """, connection, transaction))
        {
            insert.Parameters.AddWithValue(grupoId);
            insert.Parameters.AddWithValue(usuarioId);
            insert.Parameters.AddWithValue(novo.Nome);
            insert.Parameters.AddWithValue(NpgsqlDbType.Text, (object?)novo.Descricao ?? DBNull.Value);
            insert.Parameters.AddWithValue(novo.DataInicio);
            insert.Parameters.AddWithValue(novo.DataFim);
            insert.Parameters.AddWithValue(novo.DuracaoMinimaSegundos);
            insert.Parameters.AddWithValue(novo.SessoesPorSemana);
            desafioId = (Guid)(await insert.ExecuteScalarAsync(cancellationToken))!;
        }

        await transaction.CommitAsync(cancellationToken);
        return (DesafioOperacaoStatus.Concluida, desafioId);
    }
}
