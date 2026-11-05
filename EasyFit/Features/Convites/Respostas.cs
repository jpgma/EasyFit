namespace EasyFit.Features.Convites;

internal sealed record ConviteTokenRequest(string? Token);

/// <summary>O token só é entregue a proprietários e administradores do grupo.</summary>
internal sealed record ConviteResponse(string Token, string CriadoPor, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

/// <param name="Convite">Vazio quando o grupo não tem link válido.</param>
internal sealed record ConviteAtualResponse(ConviteResponse? Convite);

// Visível para quem ainda não tem conta: só o necessário para decidir se aceita. Nunca o e-mail de ninguém.
internal sealed record ConviteConsultaResponse(
    string GrupoNome,
    string ConvidadoPor,
    int TotalMembros,
    int MaxMembros,
    string? DesafioNome,
    IReadOnlyList<string> Membros);

internal sealed record EntrarGrupoResponse(Guid GrupoId);
