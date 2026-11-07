using Npgsql;
using Wrexercise.Features.Convites.Contracts;
using Wrexercise.Features.Grupos.Contracts;

namespace Wrexercise.Features.Convites;

internal static class ConsultarConvite
{
    public static async Task<IResult> HandleAsync(
        ConviteTokenRequest request,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken)
    {
        var convite = await ConsultarAsync(dataSource, request.Token, cancellationToken);
        return convite is null ? ConviteAceite.ConviteInvalido() : Results.Ok(convite);
    }

    private static async Task<ConviteConsultaResponse?> ConsultarAsync(
        NpgsqlDataSource dataSource,
        string? token,
        CancellationToken cancellationToken)
    {
        if (!ConviteAceite.IsPlausibleToken(token))
        {
            return null;
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        Guid grupoId;
        string grupoNome;
        string convidadoPor;
        string? desafioNome;
        await using (var command = new NpgsqlCommand("""
            SELECT g.id, g.nome, u.nome_exibicao,
                (SELECT d.nome
                 FROM desafios d
                 WHERE d.grupo_id = g.id
                   AND d.cancelled_at IS NULL
                   AND d.data_fim >= (now() AT TIME ZONE g.fuso_horario)::date
                 ORDER BY d.data_inicio
                 LIMIT 1)
            FROM convites_grupo c
            JOIN grupos g ON g.id = c.grupo_id
            JOIN usuarios u ON u.id = c.criador_id
            WHERE c.token = $1 AND c.revoked_at IS NULL AND c.expires_at > now()
            """, connection))
        {
            command.Parameters.AddWithValue(token!);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            grupoId = reader.GetGuid(0);
            grupoNome = reader.GetString(1);
            convidadoPor = reader.GetString(2);
            desafioNome = reader.IsDBNull(3) ? null : reader.GetString(3);
        }

        var membros = new List<string>();
        await using (var lista = new NpgsqlCommand("""
            SELECT u.nome_exibicao
            FROM membros_grupo m
            JOIN usuarios u ON u.id = m.usuario_id
            WHERE m.grupo_id = $1 AND m.removed_at IS NULL
            ORDER BY m.joined_at, m.usuario_id
            """, connection))
        {
            lista.Parameters.AddWithValue(grupoId);
            await using var reader = await lista.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                membros.Add(reader.GetString(0));
            }
        }

        return new ConviteConsultaResponse(
            grupoNome,
            convidadoPor,
            membros.Count,
            GrupoEntrada.MaxMembros,
            desafioNome,
            membros);
    }
}
