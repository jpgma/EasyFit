namespace EasyFit.Features.Desafios;

// Os campos são anuláveis porque o JSON recebido pode omiti-los; Rules valida antes de usar.
internal sealed record CriarDesafioRequest(
    string? Nome,
    string? Descricao,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    int? DuracaoMinimaSegundos,
    int? SessoesPorSemana);

/// <param name="Papel">O papel de quem consulta, para a tela decidir o que oferecer.</param>
/// <param name="Participando">Se quem consulta participa deste desafio.</param>
internal sealed record DesafioResponse(
    Guid Id,
    Guid GrupoId,
    string Nome,
    string? Descricao,
    DateOnly DataInicio,
    DateOnly DataFim,
    int DuracaoMinimaSegundos,
    int SessoesPorSemana,
    string Situacao,
    string FusoHorario,
    string Papel,
    bool Participando,
    int TotalParticipantes);

internal sealed record DesafiosResponse(IReadOnlyList<DesafioResponse> Desafios);

internal sealed record SemanaResponse(int Numero, DateOnly Inicio, DateOnly Fim);

/// <param name="SessoesVerificadas">O progresso definitivo da semana.</param>
/// <param name="SessoesProvisorias">As verificadas somadas às que ainda aguardam verificação.</param>
internal sealed record ProgressoParticipanteResponse(
    Guid UsuarioId,
    string NomeExibicao,
    int SessoesVerificadas,
    int SessoesProvisorias,
    bool MetaCumprida);

/// <param name="SemanaAtual">Vazia quando o desafio não está em andamento.</param>
internal sealed record ProgressoResponse(
    int SessoesPorSemana,
    int TotalSemanas,
    SemanaResponse? SemanaAtual,
    IReadOnlyList<ProgressoParticipanteResponse> Participantes);
