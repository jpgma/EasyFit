namespace EasyFit.Features.Convites;

internal sealed record ConviteTokenRequest(string? Token);

/// <summary>O token só aparece aqui: depois da criação, o banco guarda apenas o hash.</summary>
internal sealed record ConviteCriadoResponse(Guid Id, string Token, DateTimeOffset ExpiresAt);

internal sealed record ConviteResponse(Guid Id, string CriadoPor, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

internal sealed record ConvitesResponse(IReadOnlyList<ConviteResponse> Convites);

// Visível para quem ainda não tem conta: só o necessário para decidir se aceita.
internal sealed record ConviteConsultaResponse(string GrupoNome, string ConvidadoPor, int TotalMembros, int MaxMembros);

internal sealed record EntrarGrupoResponse(Guid GrupoId);
