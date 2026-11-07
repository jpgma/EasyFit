using Npgsql;
using Wrexercise.Infrastructure.Authentication;
using Wrexercise.Infrastructure.Files;

namespace Wrexercise.Features.Atividades;

internal static class ObterEvidencia
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        Guid evidenciaId,
        HttpContext context,
        NpgsqlDataSource dataSource,
        ArmazenamentoEvidencias armazenamento,
        CancellationToken cancellationToken)
    {
        var usuarioId = context.GetSessaoAtual().UsuarioId;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT e.caminho_relativo, e.tipo_mime
            FROM evidencias_atividade e
            JOIN atividades a ON a.id = e.atividade_id
            JOIN membros_grupo m ON m.grupo_id = a.grupo_id AND m.usuario_id = $3 AND m.removed_at IS NULL
            WHERE a.id = $1 AND e.id = $2
            """, connection);
        command.Parameters.AddWithValue(id);
        command.Parameters.AddWithValue(evidenciaId);
        command.Parameters.AddWithValue(usuarioId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return OperacaoAtividade.EvidenciaNaoEncontrada();
        }

        var caminho = reader.GetString(0);
        var tipoMime = reader.GetString(1);

        string caminhoCompleto;
        try
        {
            caminhoCompleto = armazenamento.ResolverCaminhoCompleto(caminho);
        }
        catch (EvidenciaInvalidaException)
        {
            return OperacaoAtividade.EvidenciaNaoEncontrada();
        }

        if (!File.Exists(caminhoCompleto))
        {
            return OperacaoAtividade.EvidenciaNaoEncontrada();
        }

        return Results.File(caminhoCompleto, tipoMime, enableRangeProcessing: true);
    }
}
