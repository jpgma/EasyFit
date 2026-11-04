using System.Net;
using System.Net.Http.Json;
using EasyFit.Features.Autenticacao;
using EasyFit.Features.Convites;
using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Json;

namespace EasyFit.IntegrationTests.Features.Convites;

public sealed class ConvitesRoutesTests : IAsyncLifetime
{
    private const string Consultar = "/api/convites/consultar";
    private const string Entrar = "/api/grupos/entrar";

    private ApiTestHost _host = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // O cadastro fica fechado: entrar com um convite precisa funcionar assim mesmo.
    public async ValueTask InitializeAsync() => _host = await ApiTestHost.CreateAsync(cadastroAberto: "false");

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Theory]
    [InlineData(PapelMembro.Administrador, HttpStatusCode.Created)]
    [InlineData(PapelMembro.Membro, HttpStatusCode.Forbidden)]
    public async Task Create_IsLimitedToProprietarioAndAdministrador(string papel, HttpStatusCode expected)
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var ator = await _host.AddMembroAsync(grupoId, papel);

        var response = await ator.Client.PostAsync($"/api/grupos/{grupoId}/convites", null, Token);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Create_StoresOnlyTheHash_AndExpiresInSevenDays()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);

        var response = await ana.Client.PostAsync($"/api/grupos/{grupoId}/convites", null, Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var convite = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ConviteCriadoResponse, Token);
        Assert.False(string.IsNullOrEmpty(convite!.Token));
        Assert.InRange(
            convite.ExpiresAt,
            DateTimeOffset.UtcNow.AddDays(7).AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(7).AddMinutes(1));
        Assert.Equal(0L, await _host.CountAsync(
            $"SELECT count(*) FROM convites_grupo WHERE encode(token_hash, 'escape') = '{convite.Token}'"));
    }

    [Fact]
    public async Task Create_IsRefused_WhenTheGroupIsFull()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        await FillGrupoAsync(grupoId, GrupoEntrada.MaxMembros - 1);

        var response = await ana.Client.PostAsync($"/api/grupos/{grupoId}/convites", null, Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "GRUPO_CHEIO");
    }

    [Fact]
    public async Task Endpoints_HideTheGroupFromOutsiders()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        var estranho = await _host.CreateUsuarioAsync();

        await ApiTestHost.AssertProblemAsync(
            await estranho.Client.PostAsync($"/api/grupos/{grupoId}/convites", null, Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
        await ApiTestHost.AssertProblemAsync(
            await estranho.Client.GetAsync($"/api/grupos/{grupoId}/convites", Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
        await ApiTestHost.AssertProblemAsync(
            await estranho.Client.DeleteAsync($"/api/grupos/{grupoId}/convites/{convite.Id}", Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task Endpoints_RequireASession_ExceptThePreview()
    {
        var anonimo = _host.CreateAnonimo();
        var id = Guid.NewGuid();

        await ApiTestHost.AssertProblemAsync(
            await anonimo.PostAsync($"/api/grupos/{id}/convites", null, Token),
            HttpStatusCode.Unauthorized,
            "NAO_AUTENTICADO");
        await ApiTestHost.AssertProblemAsync(
            await anonimo.GetAsync($"/api/grupos/{id}/convites", Token),
            HttpStatusCode.Unauthorized,
            "NAO_AUTENTICADO");
        await ApiTestHost.AssertProblemAsync(
            await anonimo.DeleteAsync($"/api/grupos/{id}/convites/{id}", Token),
            HttpStatusCode.Unauthorized,
            "NAO_AUTENTICADO");
        await ApiTestHost.AssertProblemAsync(
            await EntrarAsync(anonimo, "qualquer"),
            HttpStatusCode.Unauthorized,
            "NAO_AUTENTICADO");
    }

    [Fact]
    public async Task List_ShowsPendingInvitesWithoutTokens_AndIsDeniedToMembros()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var pendente = await CreateConviteAsync(ana, grupoId);
        var revogado = await CreateConviteAsync(ana, grupoId);
        await ana.Client.DeleteAsync($"/api/grupos/{grupoId}/convites/{revogado.Id}", Token);
        var membro = await _host.AddMembroAsync(grupoId);

        var response = await ana.Client.GetAsync($"/api/grupos/{grupoId}/convites", Token);
        var raw = await response.Content.ReadAsStringAsync(Token);
        Assert.DoesNotContain(pendente.Token, raw, StringComparison.Ordinal);

        var body = await ana.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}/convites", AppJsonContext.Default.ConvitesResponse, Token);
        var convite = Assert.Single(body!.Convites);
        Assert.Equal(pendente.Id, convite.Id);
        Assert.Equal("Ana", convite.CriadoPor);

        await ApiTestHost.AssertProblemAsync(
            await membro.Client.GetAsync($"/api/grupos/{grupoId}/convites", Token),
            HttpStatusCode.Forbidden,
            "ACESSO_NEGADO");
    }

    [Fact]
    public async Task Revogar_OnlyWorksWithinTheInvitesOwnGroup()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoDaAna = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoDaAna);
        var bia = await _host.CreateUsuarioAsync();
        var grupoDaBia = await _host.CreateGrupoAsync(bia);

        var response = await bia.Client.DeleteAsync($"/api/grupos/{grupoDaBia}/convites/{convite.Id}", Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "CONVITE_NAO_ENCONTRADO");
        Assert.Equal(HttpStatusCode.OK, (await ConsultarAsync(_host.CreateAnonimo(), convite.Token)).StatusCode);
    }

    [Fact]
    public async Task Consultar_ShowsThePreviewToAnonymousVisitors()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana, "Amigos");
        var convite = await CreateConviteAsync(ana, grupoId);

        var response = await ConsultarAsync(_host.CreateAnonimo(), convite.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ConviteConsultaResponse, Token);
        Assert.Equal(new ConviteConsultaResponse("Amigos", "Ana", 1, GrupoEntrada.MaxMembros), body);
    }

    [Fact]
    public async Task ExpiredUsedRevokedAndUnknownInvites_AllGetTheSameAnswer()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);

        var expirado = await CreateConviteAsync(ana, grupoId);
        await _host.ExecuteAsync(
            $"UPDATE convites_grupo SET expires_at = now() - interval '1 second' WHERE id = '{expirado.Id}'");

        var usado = await CreateConviteAsync(ana, grupoId);
        var primeira = await _host.CreateUsuarioAsync();
        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(primeira.Client, usado.Token)).StatusCode);

        var revogado = await CreateConviteAsync(ana, grupoId);
        await ana.Client.DeleteAsync($"/api/grupos/{grupoId}/convites/{revogado.Id}", Token);

        var visitante = await _host.CreateUsuarioAsync();
        foreach (var token in new[] { expirado.Token, usado.Token, revogado.Token, "desconhecido", new string('x', 500) })
        {
            await ApiTestHost.AssertProblemAsync(
                await ConsultarAsync(visitante.Client, token), HttpStatusCode.NotFound, "CONVITE_INVALIDO");
            await ApiTestHost.AssertProblemAsync(
                await EntrarAsync(visitante.Client, token), HttpStatusCode.NotFound, "CONVITE_INVALIDO");
        }

        Assert.Equal(2L, await _host.CountAsync("SELECT count(*) FROM membros_grupo WHERE removed_at IS NULL"));
    }

    [Fact]
    public async Task Entrar_AddsAMembro_AndConsumesTheInvite()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        var bia = await _host.CreateUsuarioAsync();

        var response = await EntrarAsync(bia.Client, convite.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.EntrarGrupoResponse, Token);
        Assert.Equal(grupoId, body!.GrupoId);

        var grupo = await bia.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}", AppJsonContext.Default.GrupoResponse, Token);
        Assert.Equal(PapelMembro.Membro, grupo!.Papel);
        Assert.Equal(1L, await _host.CountAsync(
            $"SELECT count(*) FROM convites_grupo WHERE id = '{convite.Id}' AND used_at IS NOT NULL AND usado_por_id = '{bia.Id}'"));
    }

    [Fact]
    public async Task Entrar_ByACurrentMember_DoesNotBurnTheInvite()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);

        var response = await EntrarAsync(ana.Client, convite.Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "JA_E_MEMBRO");
        Assert.Equal(HttpStatusCode.OK, (await ConsultarAsync(_host.CreateAnonimo(), convite.Token)).StatusCode);
    }

    [Fact]
    public async Task Entrar_InAFullGroup_DoesNotBurnTheInvite()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        await FillGrupoAsync(grupoId, GrupoEntrada.MaxMembros - 1);
        var bia = await _host.CreateUsuarioAsync();

        var response = await EntrarAsync(bia.Client, convite.Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "GRUPO_CHEIO");
        Assert.Equal(HttpStatusCode.OK, (await ConsultarAsync(_host.CreateAnonimo(), convite.Token)).StatusCode);
    }

    [Fact]
    public async Task ConcurrentAccepts_OfOneInvite_LetOnlyOnePersonIn()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        var bia = await _host.CreateUsuarioAsync();
        var caio = await _host.CreateUsuarioAsync();

        var responses = await Task.WhenAll(
            EntrarAsync(bia.Client, convite.Token),
            EntrarAsync(caio.Client, convite.Token));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        var refused = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
        await ApiTestHost.AssertProblemAsync(refused, HttpStatusCode.NotFound, "CONVITE_INVALIDO");
        Assert.Equal(2L, await _host.CountAsync("SELECT count(*) FROM membros_grupo WHERE removed_at IS NULL"));
    }

    [Fact]
    public async Task ConcurrentAccepts_ForTheLastSlot_LetOnlyOnePersonIn_AndKeepTheOtherInvite()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var primeiro = await CreateConviteAsync(ana, grupoId);
        var segundo = await CreateConviteAsync(ana, grupoId);
        await FillGrupoAsync(grupoId, GrupoEntrada.MaxMembros - 2);
        var bia = await _host.CreateUsuarioAsync();
        var caio = await _host.CreateUsuarioAsync();

        var responses = await Task.WhenAll(
            EntrarAsync(bia.Client, primeiro.Token),
            EntrarAsync(caio.Client, segundo.Token));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        var refused = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
        await ApiTestHost.AssertProblemAsync(refused, HttpStatusCode.Conflict, "GRUPO_CHEIO");
        Assert.Equal(
            GrupoEntrada.MaxMembros,
            await _host.CountAsync("SELECT count(*) FROM membros_grupo WHERE removed_at IS NULL"));
        Assert.Equal(1L, await _host.CountAsync("SELECT count(*) FROM convites_grupo WHERE used_at IS NULL"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LeavingOrBeingRemoved_RevokesThatPersonsPendingInvites(bool removed)
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var admin = await _host.AddMembroAsync(grupoId, PapelMembro.Administrador);
        var doAdmin = await CreateConviteAsync(admin, grupoId);
        var daAna = await CreateConviteAsync(ana, grupoId);

        var response = removed
            ? await ana.Client.DeleteAsync($"/api/grupos/{grupoId}/membros/{admin.Id}", Token)
            : await admin.Client.PostAsync($"/api/grupos/{grupoId}/sair", null, Token);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var anonimo = _host.CreateAnonimo();
        Assert.Equal(HttpStatusCode.NotFound, (await ConsultarAsync(anonimo, doAdmin.Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ConsultarAsync(anonimo, daAna.Token)).StatusCode);
    }

    [Fact]
    public async Task RemovedMember_ComesBackOnlyWithANewInvite_AsMembro()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var admin = await _host.AddMembroAsync(grupoId, PapelMembro.Administrador);
        await ana.Client.DeleteAsync($"/api/grupos/{grupoId}/membros/{admin.Id}", Token);
        var convite = await CreateConviteAsync(ana, grupoId);

        var response = await EntrarAsync(admin.Client, convite.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grupo = await admin.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}", AppJsonContext.Default.GrupoResponse, Token);
        Assert.Equal(PapelMembro.Membro, grupo!.Papel);
        Assert.Equal(2, grupo.TotalMembros);
    }

    [Fact]
    public async Task RemovedMembers_DoNotCountTowardsCapacity()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var removida = await _host.AddMembroAsync(grupoId);
        await FillGrupoAsync(grupoId, GrupoEntrada.MaxMembros - 2);
        await ana.Client.DeleteAsync($"/api/grupos/{grupoId}/membros/{removida.Id}", Token);
        var convite = await CreateConviteAsync(ana, grupoId);
        var bia = await _host.CreateUsuarioAsync();

        var response = await EntrarAsync(bia.Client, convite.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Registrar_WithAnInvite_WorksWhileCadastroIsClosed_AndJoinsTheGroup()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);

        var response = await RegistrarAsync(_host.CreateAnonimo(), "nova@exemplo.com", convite.Token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith("ef_session=", StringComparison.Ordinal));
        Assert.Equal(1L, await _host.CountAsync($"""
            SELECT count(*)
            FROM membros_grupo m
            JOIN usuarios u ON u.id = m.usuario_id
            WHERE u.email = 'nova@exemplo.com' AND m.grupo_id = '{grupoId}' AND m.papel = 'Membro'
            """));
        Assert.Equal(1L, await _host.CountAsync($"SELECT count(*) FROM convites_grupo WHERE id = '{convite.Id}' AND used_at IS NOT NULL"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Registrar_WithoutAnInvite_StaysClosed(string? conviteToken)
    {
        var response = await RegistrarAsync(_host.CreateAnonimo(), "nova@exemplo.com", conviteToken);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Forbidden, "CADASTRO_FECHADO");
    }

    [Fact]
    public async Task Registrar_WithABadInvite_LeavesNoAccountBehind()
    {
        var response = await RegistrarAsync(_host.CreateAnonimo(), "nova@exemplo.com", "desconhecido");

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "CONVITE_INVALIDO");
        Assert.False(response.Headers.Contains("Set-Cookie"));
        Assert.Equal(0L, await _host.CountAsync("SELECT count(*) FROM usuarios WHERE email = 'nova@exemplo.com'"));
    }

    [Fact]
    public async Task Registrar_InAFullGroup_LeavesNoAccountBehind_AndKeepsTheInvite()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        await FillGrupoAsync(grupoId, GrupoEntrada.MaxMembros - 1);

        var response = await RegistrarAsync(_host.CreateAnonimo(), "nova@exemplo.com", convite.Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "GRUPO_CHEIO");
        Assert.Equal(0L, await _host.CountAsync("SELECT count(*) FROM usuarios WHERE email = 'nova@exemplo.com'"));
        Assert.Equal(1L, await _host.CountAsync($"SELECT count(*) FROM convites_grupo WHERE id = '{convite.Id}' AND used_at IS NULL"));
    }

    [Fact]
    public async Task Registrar_WithARepeatedEmail_DoesNotBurnTheInvite()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        await _host.ExecuteAsync("UPDATE usuarios SET email = 'ana@exemplo.com'");

        var response = await RegistrarAsync(_host.CreateAnonimo(), "ana@exemplo.com", convite.Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "EMAIL_JA_CADASTRADO");
        Assert.Equal(1L, await _host.CountAsync($"SELECT count(*) FROM convites_grupo WHERE id = '{convite.Id}' AND used_at IS NULL"));
    }

    private async Task FillGrupoAsync(Guid grupoId, int quantidade)
    {
        for (var i = 0; i < quantidade; i++)
        {
            await _host.AddMembroAsync(grupoId);
        }
    }

    private static async Task<ConviteCriadoResponse> CreateConviteAsync(TestUsuario criador, Guid grupoId)
    {
        var response = await criador.Client.PostAsync($"/api/grupos/{grupoId}/convites", null, Token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ConviteCriadoResponse, Token))!;
    }

    private static Task<HttpResponseMessage> ConsultarAsync(HttpClient client, string token) =>
        client.PostAsJsonAsync(Consultar, new ConviteTokenRequest(token), AppJsonContext.Default.ConviteTokenRequest, Token);

    private static Task<HttpResponseMessage> EntrarAsync(HttpClient client, string token) =>
        client.PostAsJsonAsync(Entrar, new ConviteTokenRequest(token), AppJsonContext.Default.ConviteTokenRequest, Token);

    private static Task<HttpResponseMessage> RegistrarAsync(HttpClient client, string email, string? conviteToken) =>
        client.PostAsJsonAsync(
            "/api/autenticacao/registrar",
            new RegistrarRequest("Nova", email, "senha-segura", conviteToken),
            AppJsonContext.Default.RegistrarRequest,
            Token);
}
