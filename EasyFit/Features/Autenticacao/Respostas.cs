namespace EasyFit.Features.Autenticacao;

internal sealed record UsuarioResponse(Guid Id, string NomeExibicao, string Email);

internal sealed record SessaoResponse(UsuarioResponse Usuario);
