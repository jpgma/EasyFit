using System.Net;
using System.Net.Http.Json;
using EasyFit.Features.Grupos;
using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Json;

namespace EasyFit.IntegrationTests.Features.Grupos;

public sealed class GruposRoutesTests : IAsyncLifetime
{
    private ApiTestHost _host = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _host = await ApiTestHost.CreateAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task Create_MakesTheCreatorProprietario_AndListsOnlyOwnGroups()
    {
        var ana = await _host.CreateUsuarioAsync();
        var bia = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana, "  Amigos  ");
        await _host.CreateGrupoAsync(bia);

        var response = await ana.Client.GetAsync("/api/grupos", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.GruposResponse, Token);
        var grupo = Assert.Single(body!.Grupos);
        Assert.Equal(new GrupoResumoResponse(grupoId, "Amigos", PapelMembro.Proprietario, 1), grupo);
    }

    [Theory]
    [InlineData("", "America/Sao_Paulo")]
    [InlineData(null, "America/Sao_Paulo")]
    [InlineData("Amigos", "Marte/Olympus_Mons")]
    [InlineData("Amigos", "america/sao_paulo")]
    [InlineData("Amigos", "")]
    [InlineData("Amigos", null)]
    public async Task Create_RejectsInvalidData(string? nome, string? fusoHorario)
    {
        var ana = await _host.CreateUsuarioAsync();

        var response = await ana.Client.PostAsJsonAsync(
            "/api/grupos",
            new CriarGrupoRequest(nome, fusoHorario),
            AppJsonContext.Default.CriarGrupoRequest,
            Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "DADOS_INVALIDOS");
        Assert.Equal(0L, await _host.CountAsync("SELECT count(*) FROM grupos"));
    }

    [Fact]
    public async Task EveryEndpoint_RequiresASession()
    {
        var anonimo = _host.CreateAnonimo();
        var id = Guid.NewGuid();

        foreach (var (method, path) in AllEndpoints(id, Guid.NewGuid()))
        {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(new { }) };
            var response = await anonimo.SendAsync(request, Token);
            await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "NAO_AUTENTICADO");
        }
    }

    [Fact]
    public async Task EveryEndpoint_HidesTheGroupFromOutsiders_AndRemovedMembers()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var alvo = await _host.AddMembroAsync(grupoId);
        var estranho = await _host.CreateUsuarioAsync();
        var removida = await _host.AddMembroAsync(grupoId);
        Assert.Equal(HttpStatusCode.NoContent, (await ana.Client.DeleteAsync(MembroPath(grupoId, removida.Id), Token)).StatusCode);

        foreach (var usuario in new[] { estranho, removida })
        {
            await AssertGrupoHiddenAsync(usuario, grupoId, alvo.Id);
        }

        Assert.Equal(2L, await _host.CountAsync("SELECT count(*) FROM membros_grupo WHERE removed_at IS NULL"));
    }

    [Fact]
    public async Task Get_ReturnsTheCallersPapel_AndMembros_WithoutEmails()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId, PapelMembro.Administrador, "Bia");
        await _host.AddMembroAsync(grupoId, PapelMembro.Membro, "Caio");

        var grupo = await bia.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}", AppJsonContext.Default.GrupoResponse, Token);
        Assert.Equal(PapelMembro.Administrador, grupo!.Papel);
        Assert.Equal(3, grupo.TotalMembros);
        Assert.Equal(10, grupo.MaxMembros);
        Assert.Equal("America/Sao_Paulo", grupo.FusoHorario);

        var response = await bia.Client.GetAsync($"/api/grupos/{grupoId}/membros", Token);
        var raw = await response.Content.ReadAsStringAsync(Token);
        Assert.DoesNotContain("email", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@exemplo.com", raw, StringComparison.OrdinalIgnoreCase);

        var membros = await bia.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}/membros", AppJsonContext.Default.MembrosResponse, Token);
        Assert.Equal(["Ana", "Bia", "Caio"], membros!.Membros.Select(membro => membro.NomeExibicao));
    }

    [Theory]
    [InlineData(PapelMembro.Administrador, PapelMembro.Membro)]
    [InlineData(PapelMembro.Membro, PapelMembro.Membro)]
    public async Task AlterarPapel_IsOnlyForTheProprietario(string papelAtor, string papelAlvo)
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var ator = await _host.AddMembroAsync(grupoId, papelAtor);
        var alvo = await _host.AddMembroAsync(grupoId, papelAlvo);

        var response = await AlterarPapelAsync(ator, grupoId, alvo.Id, PapelMembro.Administrador);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Forbidden, "ACESSO_NEGADO");
        Assert.Equal(papelAlvo, await GetPapelAsync(ana, grupoId, alvo.Id));
    }

    [Fact]
    public async Task AlterarPapel_PromotesAndDemotes()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId);

        var promote = await AlterarPapelAsync(ana, grupoId, bia.Id, PapelMembro.Administrador);
        Assert.Equal(HttpStatusCode.NoContent, promote.StatusCode);
        Assert.Equal(PapelMembro.Administrador, await GetPapelAsync(ana, grupoId, bia.Id));

        var demote = await AlterarPapelAsync(ana, grupoId, bia.Id, PapelMembro.Membro);
        Assert.Equal(HttpStatusCode.NoContent, demote.StatusCode);
        Assert.Equal(PapelMembro.Membro, await GetPapelAsync(ana, grupoId, bia.Id));
    }

    [Theory]
    [InlineData(PapelMembro.Proprietario)]
    [InlineData("Dono")]
    [InlineData("")]
    [InlineData(null)]
    public async Task AlterarPapel_RejectsPapelThatCannotBeAssigned(string? papel)
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId);

        var response = await AlterarPapelAsync(ana, grupoId, bia.Id, papel);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.BadRequest, "DADOS_INVALIDOS");
    }

    [Fact]
    public async Task AlterarPapel_RefusesToDemoteTheProprietario()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);

        var response = await AlterarPapelAsync(ana, grupoId, ana.Id, PapelMembro.Membro);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Forbidden, "ACESSO_NEGADO");
    }

    [Theory]
    [InlineData(PapelMembro.Administrador, PapelMembro.Membro, HttpStatusCode.NoContent)]
    [InlineData(PapelMembro.Administrador, PapelMembro.Administrador, HttpStatusCode.Forbidden)]
    [InlineData(PapelMembro.Membro, PapelMembro.Membro, HttpStatusCode.Forbidden)]
    public async Task RemoverMembro_RespectsTheHierarchy(string papelAtor, string papelAlvo, HttpStatusCode expected)
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var ator = await _host.AddMembroAsync(grupoId, papelAtor);
        var alvo = await _host.AddMembroAsync(grupoId, papelAlvo);

        var response = await ator.Client.DeleteAsync(MembroPath(grupoId, alvo.Id), Token);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task RemoverMembro_ByTheProprietario_WorksOnAnyoneElse_ButNeverOnThemselves()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var admin = await _host.AddMembroAsync(grupoId, PapelMembro.Administrador);

        var removeAdmin = await ana.Client.DeleteAsync(MembroPath(grupoId, admin.Id), Token);
        Assert.Equal(HttpStatusCode.NoContent, removeAdmin.StatusCode);

        var removeSelf = await ana.Client.DeleteAsync(MembroPath(grupoId, ana.Id), Token);
        await ApiTestHost.AssertProblemAsync(removeSelf, HttpStatusCode.Forbidden, "ACESSO_NEGADO");
    }

    [Fact]
    public async Task RemoverMembro_OfSomeoneOutsideTheGroup_IsNotFound()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var estranho = await _host.CreateUsuarioAsync();

        var response = await ana.Client.DeleteAsync(MembroPath(grupoId, estranho.Id), Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "MEMBRO_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task RemoverMembro_KeepsTheRow_ForTheGroupsHistory()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId);

        await ana.Client.DeleteAsync(MembroPath(grupoId, bia.Id), Token);

        Assert.Equal(1L, await _host.CountAsync(
            $"SELECT count(*) FROM membros_grupo WHERE usuario_id = '{bia.Id}' AND removed_at IS NOT NULL"));
    }

    [Fact]
    public async Task Sair_IsRefusedToTheProprietario_UntilOwnershipIsTransferred()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId);

        var refused = await ana.Client.PostAsync($"/api/grupos/{grupoId}/sair", null, Token);
        await ApiTestHost.AssertProblemAsync(refused, HttpStatusCode.Conflict, "PROPRIETARIO_NAO_PODE_SAIR");

        var transfer = await TransferirAsync(ana, grupoId, bia.Id);
        Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);
        Assert.Equal(PapelMembro.Proprietario, await GetPapelAsync(bia, grupoId, bia.Id));
        Assert.Equal(PapelMembro.Administrador, await GetPapelAsync(bia, grupoId, ana.Id));

        var left = await ana.Client.PostAsync($"/api/grupos/{grupoId}/sair", null, Token);
        Assert.Equal(HttpStatusCode.NoContent, left.StatusCode);
        var after = await ana.Client.GetAsync($"/api/grupos/{grupoId}", Token);
        await ApiTestHost.AssertProblemAsync(after, HttpStatusCode.NotFound, "GRUPO_NAO_ENCONTRADO");
    }

    [Theory]
    [InlineData(PapelMembro.Administrador)]
    [InlineData(PapelMembro.Membro)]
    public async Task Sair_WorksForOtherPapeis(string papel)
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId, papel);

        var response = await bia.Client.PostAsync($"/api/grupos/{grupoId}/sair", null, Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData(PapelMembro.Administrador)]
    [InlineData(PapelMembro.Membro)]
    public async Task Transferir_IsOnlyForTheProprietario(string papel)
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var ator = await _host.AddMembroAsync(grupoId, papel);

        var response = await TransferirAsync(ator, grupoId, ator.Id);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Forbidden, "ACESSO_NEGADO");
        Assert.Equal(PapelMembro.Proprietario, await GetPapelAsync(ana, grupoId, ana.Id));
    }

    [Fact]
    public async Task Transferir_RejectsSelf_AndNonMembers()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var estranho = await _host.CreateUsuarioAsync();

        await ApiTestHost.AssertProblemAsync(
            await TransferirAsync(ana, grupoId, ana.Id), HttpStatusCode.BadRequest, "DADOS_INVALIDOS");
        await ApiTestHost.AssertProblemAsync(
            await TransferirAsync(ana, grupoId, estranho.Id), HttpStatusCode.NotFound, "MEMBRO_NAO_ENCONTRADO");
        Assert.Equal(PapelMembro.Proprietario, await GetPapelAsync(ana, grupoId, ana.Id));
    }

    [Fact]
    public async Task MalformedGroupId_GetsTheJsonNotFound()
    {
        var ana = await _host.CreateUsuarioAsync();

        var response = await ana.Client.GetAsync("/api/grupos/nao-e-guid", Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "ROTA_NAO_ENCONTRADA");
    }

    private static (HttpMethod Method, string Path)[] AllEndpoints(Guid grupoId, Guid usuarioId) =>
    [
        (HttpMethod.Get, "/api/grupos"),
        (HttpMethod.Post, "/api/grupos"),
        (HttpMethod.Get, $"/api/grupos/{grupoId}"),
        (HttpMethod.Get, $"/api/grupos/{grupoId}/membros"),
        (HttpMethod.Put, $"/api/grupos/{grupoId}/membros/{usuarioId}/papel"),
        (HttpMethod.Delete, MembroPath(grupoId, usuarioId)),
        (HttpMethod.Post, $"/api/grupos/{grupoId}/sair"),
        (HttpMethod.Post, $"/api/grupos/{grupoId}/transferir"),
    ];

    private static async Task AssertGrupoHiddenAsync(TestUsuario usuario, Guid grupoId, Guid alvoId)
    {
        await ApiTestHost.AssertProblemAsync(
            await usuario.Client.GetAsync($"/api/grupos/{grupoId}", Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
        await ApiTestHost.AssertProblemAsync(
            await usuario.Client.GetAsync($"/api/grupos/{grupoId}/membros", Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
        await ApiTestHost.AssertProblemAsync(
            await AlterarPapelAsync(usuario, grupoId, alvoId, PapelMembro.Administrador),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
        await ApiTestHost.AssertProblemAsync(
            await usuario.Client.DeleteAsync(MembroPath(grupoId, alvoId), Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
        await ApiTestHost.AssertProblemAsync(
            await usuario.Client.PostAsync($"/api/grupos/{grupoId}/sair", null, Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
        await ApiTestHost.AssertProblemAsync(
            await TransferirAsync(usuario, grupoId, alvoId),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
    }

    private static string MembroPath(Guid grupoId, Guid usuarioId) => $"/api/grupos/{grupoId}/membros/{usuarioId}";

    private static Task<HttpResponseMessage> AlterarPapelAsync(
        TestUsuario ator,
        Guid grupoId,
        Guid alvoId,
        string? papel) =>
        ator.Client.PutAsJsonAsync(
            $"{MembroPath(grupoId, alvoId)}/papel",
            new AlterarPapelRequest(papel),
            AppJsonContext.Default.AlterarPapelRequest,
            Token);

    private static Task<HttpResponseMessage> TransferirAsync(TestUsuario ator, Guid grupoId, Guid alvoId) =>
        ator.Client.PostAsJsonAsync(
            $"/api/grupos/{grupoId}/transferir",
            new TransferirGrupoRequest(alvoId),
            AppJsonContext.Default.TransferirGrupoRequest,
            Token);

    private static async Task<string> GetPapelAsync(TestUsuario leitor, Guid grupoId, Guid usuarioId)
    {
        var membros = await leitor.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}/membros", AppJsonContext.Default.MembrosResponse, Token);
        return membros!.Membros.Single(membro => membro.UsuarioId == usuarioId).Papel;
    }
}
