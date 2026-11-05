namespace EasyFit.Features.Reacoes;

internal sealed record ReacaoRequest(string? Tipo);

internal sealed record RevisaoRequest(string? Decisao, string? Justificativa);

internal sealed record ReacaoItemResponse(
    Guid UsuarioId,
    string NomeExibicao,
    string Tipo,
    DateTimeOffset UpdatedAt);

internal sealed record ReacoesResponse(
    int TotalRevisores,
    int RejeicoesNecessarias,
    int Kudos,
    int Suspeito,
    int Rejeitar,
    string? SuaReacao,
    bool VoceERevisor,
    bool JanelaAberta,
    DateTimeOffset RevisaoEncerraEm,
    IReadOnlyList<ReacaoItemResponse> Reacoes);

internal sealed record ComentarioRequest(string? Texto);

internal sealed record ComentarioResponse(
    Guid Id,
    Guid UsuarioId,
    string NomeExibicao,
    string Texto,
    DateTimeOffset CreatedAt);

internal sealed record ComentariosResponse(IReadOnlyList<ComentarioResponse> Comentarios);

internal sealed record RevisaoRegistradaResponse(
    string SituacaoVerificacao,
    string Decisao,
    DateTimeOffset RegistradaEm);
