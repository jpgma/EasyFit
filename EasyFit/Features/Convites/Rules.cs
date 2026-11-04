using EasyFit.Features.Grupos.Contracts;

namespace EasyFit.Features.Convites;

internal static class ConvitesRules
{
    public static readonly TimeSpan Validade = TimeSpan.FromDays(7);

    public static bool CanGerenciarConvites(string papel) =>
        papel is PapelMembro.Proprietario or PapelMembro.Administrador;
}
