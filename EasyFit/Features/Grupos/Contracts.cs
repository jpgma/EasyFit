namespace EasyFit.Features.Grupos;

// Os campos são anuláveis porque o JSON recebido pode omiti-los; Rules valida antes de usar.
internal sealed record CriarGrupoRequest(string? Nome, string? FusoHorario);

internal sealed record AlterarPapelRequest(string? Papel);

internal sealed record TransferirGrupoRequest(Guid? UsuarioId);

internal sealed record GrupoResumoResponse(Guid Id, string Nome, string Papel, int TotalMembros);

internal sealed record GruposResponse(IReadOnlyList<GrupoResumoResponse> Grupos);

internal sealed record GrupoResponse(
    Guid Id,
    string Nome,
    string FusoHorario,
    string Papel,
    int TotalMembros,
    int MaxMembros);

// O e-mail fica de fora de propósito: os colegas de grupo só precisam do nome.
internal sealed record MembroResponse(Guid UsuarioId, string NomeExibicao, string Papel, DateTimeOffset JoinedAt);

internal sealed record MembrosResponse(IReadOnlyList<MembroResponse> Membros);
