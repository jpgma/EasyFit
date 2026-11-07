using Npgsql;
using Wrexercise.Features.Grupos.Contracts;
using Wrexercise.Infrastructure.Authentication;
using Wrexercise.Infrastructure.Json;

namespace Wrexercise.Features.Grupos;

// Os campos são anuláveis porque o JSON recebido pode omiti-los; ValidacaoGrupo valida antes de usar.
internal sealed record CriarGrupoRequest(string? Nome, string? FusoHorario);

internal static class CriarGrupo
{
    public static async Task<IResult> HandleAsync(
        CriarGrupoRequest request,
        HttpContext context,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        if (!ValidacaoGrupo.IsValidNome(request.Nome)
            || !ValidacaoGrupo.IsPlausibleFusoHorario(request.FusoHorario)
            || !await IsFusoHorarioValidoAsync(dataSource, request.FusoHorario!, cancellationToken))
        {
            return MembrosGrupo.InvalidData();
        }

        var grupo = await ExecutarAsync(
            dataSource,
            ValidacaoGrupo.NormalizeNome(request.Nome!),
            request.FusoHorario!,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        return Results.Json(grupo, AppJsonContext.Default.GrupoResponse, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<GrupoResponse> ExecutarAsync(
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

    // O PostgreSQL decide: com InvariantGlobalization, o .NET não reconhece fusos IANA no Windows.
    private static async Task<bool> IsFusoHorarioValidoAsync(
        NpgsqlDataSource dataSource,
        string fusoHorario,
        CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_timezone_names WHERE name = $1)");
        command.Parameters.AddWithValue(fusoHorario);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
