namespace EasyFit.Features.Autenticacao;

internal static class ValidacaoConta
{
    public const int MinSenhaLength = 8;

    // Limita o custo do PBKDF2 para uma senha enviada de propósito muito longa.
    public const int MaxSenhaLength = 128;

    private const int MaxNomeExibicaoLength = 80;
    private const int MaxEmailLength = 254;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string NormalizeNomeExibicao(string nomeExibicao) => nomeExibicao.Trim();

    public static bool IsValidNomeExibicao(string? nomeExibicao)
    {
        var length = nomeExibicao?.Trim().Length ?? 0;
        return length is > 0 and <= MaxNomeExibicaoLength;
    }

    public static bool IsValidEmail(string? email)
    {
        var value = email?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > MaxEmailLength || value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        // Validação propositalmente simples: sem envio de e-mail, nada confirmaria o endereço de qualquer forma.
        var at = value.IndexOf('@');
        return at > 0 && at == value.LastIndexOf('@') && at < value.Length - 1;
    }

    public static bool IsValidSenha(string? senha) =>
        senha?.Length is >= MinSenhaLength and <= MaxSenhaLength;
}
