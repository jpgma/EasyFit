using EasyFit.Features.Grupos.Contracts;

namespace EasyFit.Features.Grupos;

internal static class GruposRules
{
    private const int MaxNomeLength = 80;
    private const int MaxFusoHorarioLength = 64;

    public static string NormalizeNome(string nome) => nome.Trim();

    public static bool IsValidNome(string? nome)
    {
        var length = nome?.Trim().Length ?? 0;
        return length is > 0 and <= MaxNomeLength;
    }

    /// <summary>Só descarta o que nem precisa ir ao banco, que é quem conhece os fusos válidos.</summary>
    public static bool IsPlausibleFusoHorario(string? fusoHorario) =>
        fusoHorario?.Length is > 0 and <= MaxFusoHorarioLength;

    /// <summary>A propriedade só muda de mãos pela transferência, nunca pela alteração de papel.</summary>
    public static bool IsPapelAtribuivel(string? papel) =>
        papel is PapelMembro.Administrador or PapelMembro.Membro;

    public static bool CanAlterarPapel(string ator, string alvo) =>
        ator == PapelMembro.Proprietario && alvo != PapelMembro.Proprietario;

    /// <summary>Ninguém remove a si mesmo por aqui: para isso existe a saída do grupo.</summary>
    public static bool CanRemoverMembro(string ator, string alvo) => ator switch
    {
        PapelMembro.Proprietario => alvo != PapelMembro.Proprietario,
        PapelMembro.Administrador => alvo == PapelMembro.Membro,
        _ => false,
    };

    public static bool CanSair(string papel) => papel != PapelMembro.Proprietario;

    public static bool CanTransferir(string papel) => papel == PapelMembro.Proprietario;
}
