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

internal sealed record RevisaoRegistradaResponse(
    string SituacaoVerificacao,
    string Decisao,
    DateTimeOffset RegistradaEm);
