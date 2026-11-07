using Npgsql;
using Wrexercise.Features.Grupos.Contracts;
using Wrexercise.Infrastructure.Authentication;

namespace Wrexercise.Features.Convites;

internal static class ValidacaoConvite
{
    public static readonly TimeSpan Validade = TimeSpan.FromDays(7);

    public static bool CanGerenciarConvites(string papel) =>
        papel is PapelMembro.Proprietario or PapelMembro.Administrador;

    public static async Task<IResult?> CheckGerenciarAsync(
        HttpContext context,
        NpgsqlDataSource dataSource,
        Guid grupoId,
        CancellationToken cancellationToken)
    {
        var papel = await GrupoAcesso.GetPapelAsync(
            dataSource,
            grupoId,
            context.GetSessaoAtual().UsuarioId,
            cancellationToken);

        if (papel is null)
        {
            return GrupoAcesso.GrupoNaoEncontrado();
        }

        return CanGerenciarConvites(papel) ? null : GrupoAcesso.AcessoNegado();
    }
}
