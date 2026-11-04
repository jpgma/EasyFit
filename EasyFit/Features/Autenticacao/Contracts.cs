namespace EasyFit.Features.Autenticacao;

// Os campos são anuláveis porque o JSON recebido pode omiti-los; Rules valida antes de usar.
internal sealed record RegistrarRequest(string? NomeExibicao, string? Email, string? Senha);

internal sealed record EntrarRequest(string? Email, string? Senha);

internal sealed record UsuarioResponse(Guid Id, string NomeExibicao, string Email);

internal sealed record SessaoResponse(UsuarioResponse Usuario);
