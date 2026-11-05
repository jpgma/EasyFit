namespace EasyFit.Features.Progresso;

internal sealed record ClassificacaoParticipanteResponse(
    int Posicao,
    Guid UsuarioId,
    string NomeExibicao,
    int SemanasCumpridas,
    int SessoesVerificadas,
    int SequenciaSemanas);

internal sealed record ClassificacaoResponse(
    int SessoesPorSemana,
    int TotalSemanas,
    IReadOnlyList<ClassificacaoParticipanteResponse> Participantes);

internal sealed record HistoricoSemanaResponse(
    int Numero,
    DateOnly Inicio,
    DateOnly Fim,
    int SessoesVerificadas,
    int SessoesProvisorias,
    bool MetaCumprida);

internal sealed record HistoricoSemanalResponse(
    int SessoesPorSemana,
    int TotalSemanas,
    IReadOnlyList<HistoricoSemanaResponse> Semanas);

internal sealed record DesafioDestaqueResponse(
    Guid Id,
    string Nome,
    string Situacao);

internal sealed record ProgressoResumoResponse(
    int SessoesVerificadas,
    int SessoesProvisorias,
    bool MetaCumprida,
    int? SemanaNumero);

internal sealed record ClassificacaoResumoItemResponse(
    int Posicao,
    string NomeExibicao,
    int SemanasCumpridas);

internal sealed record ResumoGrupoResponse(
    Guid GrupoId,
    DesafioDestaqueResponse? Desafio,
    int? SessoesPorSemana,
    ProgressoResumoResponse? MeuProgresso,
    IReadOnlyList<ClassificacaoResumoItemResponse> Classificacao);
