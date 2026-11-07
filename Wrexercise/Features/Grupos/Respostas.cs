namespace Wrexercise.Features.Grupos;

internal sealed record GrupoResponse(
    Guid Id,
    string Nome,
    string FusoHorario,
    string Papel,
    int TotalMembros,
    int MaxMembros);

// O e-mail fica de fora de propósito: os colegas de grupo só precisam do nome.
internal sealed record MembroResponse(Guid UsuarioId, string NomeExibicao, string Papel, DateTimeOffset JoinedAt);
