using System.Net;
using System.Net.Http.Json;
using Wrexercise.Features.Autenticacao;
using Wrexercise.Features.Convites;
using Wrexercise.Features.Grupos.Contracts;
using Wrexercise.Infrastructure.Json;

namespace Wrexercise.IntegrationTests.Features.Convites;

public sealed class ConvitesRoutesTests : IAsyncLifetime
{
    private const string Consultar = "/api/convites/consultar";
    private const string Entrar = "/api/grupos/entrar";

    private ApiTestHost _host = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // O cadastro fica fechado: entrar com um convite precisa funcionar assim mesmo.
    public async ValueTask InitializeAsync() => _host = await ApiTestHost.CreateAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Theory]
    [InlineData(PapelMembro.Administrador, HttpStatusCode.Created)]
    [InlineData(PapelMembro.Membro, HttpStatusCode.Forbidden)]
    public async Task Create_IsLimitedToProprietarioAndAdministrador(string papel, HttpStatusCode expected)
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var ator = await _host.AddMembroAsync(grupoId, papel);

        var response = await ator.Client.PostAsync($"/api/grupos/{grupoId}/convite", null, Token);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Create_ExpiresInSevenDays_AndTheLinkStaysVisibleToAdmins()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var admin = await _host.AddMembroAsync(grupoId, PapelMembro.Administrador);

        Assert.Null((await GetConviteAsync(ana, grupoId)).Convite);

        var convite = await CreateConviteAsync(ana, grupoId);

        Assert.False(string.IsNullOrEmpty(convite.Token));
        Assert.Equal("Ana", convite.CriadoPor);
        Assert.InRange(
            convite.ExpiresAt,
            DateTimeOffset.UtcNow.AddDays(7).AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(7).AddMinutes(1));
        Assert.Equal(convite.Token, (await GetConviteAsync(admin, grupoId)).Convite?.Token);
    }

    [Fact]
    public async Task Create_ReplacesThePreviousLink()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var antigo = await CreateConviteAsync(ana, grupoId);

        var novo = await CreateConviteAsync(ana, grupoId);

        Assert.NotEqual(antigo.Token, novo.Token);
        Assert.Equal(novo.Token, (await GetConviteAsync(ana, grupoId)).Convite?.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await ConsultarAsync(_host.CreateAnonimo(), antigo.Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ConsultarAsync(_host.CreateAnonimo(), novo.Token)).StatusCode);
    }

    [Fact]
    public async Task Create_ReplacesAnExpiredLink()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        await CreateConviteAsync(ana, grupoId);
        await _host.ExecuteAsync("UPDATE convites_grupo SET expires_at = now() - interval '1 second'");

        Assert.Null((await GetConviteAsync(ana, grupoId)).Convite);

        var novo = await CreateConviteAsync(ana, grupoId);
        Assert.Equal(novo.Token, (await GetConviteAsync(ana, grupoId)).Convite?.Token);
    }

    [Fact]
    public async Task ConcurrentCreates_LeaveOneActiveLink()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            ana.Client.PostAsync($"/api/grupos/{grupoId}/convite", null, Token)));

        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(1L, await _host.CountAsync("SELECT count(*) FROM convites_grupo WHERE revoked_at IS NULL"));
    }

    [Fact]
    public async Task Create_IsRefused_WhenTheGroupIsFull()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        await FillGrupoAsync(grupoId, GrupoEntrada.MaxMembros - 1);

        var response = await ana.Client.PostAsync($"/api/grupos/{grupoId}/convite", null, Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "GRUPO_CHEIO");
    }

    [Fact]
    public async Task Get_And_Delete_AreDeniedToMembros()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        await CreateConviteAsync(ana, grupoId);
        var membro = await _host.AddMembroAsync(grupoId);

        await ApiTestHost.AssertProblemAsync(
            await membro.Client.GetAsync($"/api/grupos/{grupoId}/convite", Token),
            HttpStatusCode.Forbidden,
            "ACESSO_NEGADO");
        await ApiTestHost.AssertProblemAsync(
            await membro.Client.DeleteAsync($"/api/grupos/{grupoId}/convite", Token),
            HttpStatusCode.Forbidden,
            "ACESSO_NEGADO");
    }

    [Fact]
    public async Task Endpoints_HideTheGroupFromOutsiders()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        await CreateConviteAsync(ana, grupoId);
        var estranho = await _host.CreateUsuarioAsync();

        await ApiTestHost.AssertProblemAsync(
            await estranho.Client.PostAsync($"/api/grupos/{grupoId}/convite", null, Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
        await ApiTestHost.AssertProblemAsync(
            await estranho.Client.GetAsync($"/api/grupos/{grupoId}/convite", Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
        await ApiTestHost.AssertProblemAsync(
            await estranho.Client.DeleteAsync($"/api/grupos/{grupoId}/convite", Token),
            HttpStatusCode.NotFound,
            "GRUPO_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task Endpoints_RequireASession_ExceptThePreview()
    {
        var anonimo = _host.CreateAnonimo();
        var id = Guid.NewGuid();

        await ApiTestHost.AssertProblemAsync(
            await anonimo.PostAsync($"/api/grupos/{id}/convite", null, Token),
            HttpStatusCode.Unauthorized,
            "NAO_AUTENTICADO");
        await ApiTestHost.AssertProblemAsync(
            await anonimo.GetAsync($"/api/grupos/{id}/convite", Token),
            HttpStatusCode.Unauthorized,
            "NAO_AUTENTICADO");
        await ApiTestHost.AssertProblemAsync(
            await anonimo.DeleteAsync($"/api/grupos/{id}/convite", Token),
            HttpStatusCode.Unauthorized,
            "NAO_AUTENTICADO");
        await ApiTestHost.AssertProblemAsync(
            await EntrarAsync(anonimo, "qualquer"),
            HttpStatusCode.Unauthorized,
            "NAO_AUTENTICADO");
    }

    [Fact]
    public async Task Revogar_WithoutAnActiveLink_IsNotFound()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);

        var response = await ana.Client.DeleteAsync($"/api/grupos/{grupoId}/convite", Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.NotFound, "CONVITE_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task Consultar_ShowsThePreviewToAnonymousVisitors_WithoutEmails()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana, "Amigos");
        await _host.AddMembroAsync(grupoId, nome: "Bia");
        var convite = await CreateConviteAsync(ana, grupoId);

        var response = await ConsultarAsync(_host.CreateAnonimo(), convite.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync(Token);
        Assert.DoesNotContain("@", raw, StringComparison.Ordinal);
        var body = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ConviteConsultaResponse, Token);
        Assert.Equal("Amigos", body!.GrupoNome);
        Assert.Equal("Ana", body.ConvidadoPor);
        Assert.Equal(2, body.TotalMembros);
        Assert.Equal(GrupoEntrada.MaxMembros, body.MaxMembros);
        Assert.Null(body.DesafioNome);
        Assert.Equal(["Ana", "Bia"], body.Membros);
    }

    [Fact]
    public async Task Consultar_ShowsTheCurrentDesafio_NeverACanceledOrFinishedOne()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        const string HojeNoGrupo = "(now() AT TIME ZONE 'America/Sao_Paulo')::date";
        await _host.ExecuteAsync($"""
            INSERT INTO desafios (
                grupo_id, criador_id, nome, data_inicio, data_fim,
                duracao_minima_segundos, sessoes_por_semana, cancelled_at)
            VALUES
                ('{grupoId}', '{ana.Id}', 'Encerrado',
                 date_trunc('week', {HojeNoGrupo})::date - 14, date_trunc('week', {HojeNoGrupo})::date - 8, 3600, 4, NULL),
                ('{grupoId}', '{ana.Id}', 'Cancelado',
                 date_trunc('week', {HojeNoGrupo})::date, date_trunc('week', {HojeNoGrupo})::date + 6, 3600, 4, now()),
                ('{grupoId}', '{ana.Id}', 'Atual',
                 date_trunc('week', {HojeNoGrupo})::date, date_trunc('week', {HojeNoGrupo})::date + 6, 3600, 4, NULL),
                ('{grupoId}', '{ana.Id}', 'Próximo',
                 date_trunc('week', {HojeNoGrupo})::date + 7, date_trunc('week', {HojeNoGrupo})::date + 13, 3600, 4, NULL)
            """);

        var response = await ConsultarAsync(_host.CreateAnonimo(), convite.Token);

        var body = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ConviteConsultaResponse, Token);
        Assert.Equal("Atual", body!.DesafioNome);
    }

    [Fact]
    public async Task ExpiredRevokedAndUnknownInvites_AllGetTheSameAnswer()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoExpirado = await _host.CreateGrupoAsync(ana);
        var grupoRevogado = await _host.CreateGrupoAsync(ana);

        var expirado = await CreateConviteAsync(ana, grupoExpirado);
        await _host.ExecuteAsync(
            $"UPDATE convites_grupo SET expires_at = now() - interval '1 second' WHERE grupo_id = '{grupoExpirado}'");

        var revogado = await CreateConviteAsync(ana, grupoRevogado);
        await ana.Client.DeleteAsync($"/api/grupos/{grupoRevogado}/convite", Token);

        var visitante = await _host.CreateUsuarioAsync();
        foreach (var token in new[] { expirado.Token, revogado.Token, "desconhecido", new string('x', 500) })
        {
            await ApiTestHost.AssertProblemAsync(
                await ConsultarAsync(visitante.Client, token), HttpStatusCode.NotFound, "CONVITE_INVALIDO");
            await ApiTestHost.AssertProblemAsync(
                await EntrarAsync(visitante.Client, token), HttpStatusCode.NotFound, "CONVITE_INVALIDO");
        }

        Assert.Equal(2L, await _host.CountAsync("SELECT count(*) FROM membros_grupo WHERE removed_at IS NULL"));
    }

    [Fact]
    public async Task Entrar_AddsAMembro_AndTheLinkKeepsWorking()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        var bia = await _host.CreateUsuarioAsync();
        var caio = await _host.CreateUsuarioAsync();

        var response = await EntrarAsync(bia.Client, convite.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.EntrarGrupoResponse, Token);
        Assert.Equal(grupoId, body!.GrupoId);

        var grupo = await bia.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}", AppJsonContext.Default.GrupoResponse, Token);
        Assert.Equal(PapelMembro.Membro, grupo!.Papel);

        Assert.Equal(HttpStatusCode.OK, (await EntrarAsync(caio.Client, convite.Token)).StatusCode);
    }

    [Fact]
    public async Task Entrar_ByACurrentMember_IsRefused()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);

        var response = await EntrarAsync(ana.Client, convite.Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "JA_E_MEMBRO");
    }

    [Fact]
    public async Task Entrar_InAFullGroup_IsRefused()
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
    public async Task ConcurrentAccepts_ForTheLastSlot_LetOnlyOnePersonIn()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        await FillGrupoAsync(grupoId, GrupoEntrada.MaxMembros - 2);
        var bia = await _host.CreateUsuarioAsync();
        var caio = await _host.CreateUsuarioAsync();

        var responses = await Task.WhenAll(
            EntrarAsync(bia.Client, convite.Token),
            EntrarAsync(caio.Client, convite.Token));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        var refused = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
        await ApiTestHost.AssertProblemAsync(refused, HttpStatusCode.Conflict, "GRUPO_CHEIO");
        Assert.Equal(
            GrupoEntrada.MaxMembros,
            await _host.CountAsync("SELECT count(*) FROM membros_grupo WHERE removed_at IS NULL"));
    }

    [Fact]
    public async Task Leaving_RevokesTheLink_OnlyIfThatPersonCreatedIt()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoDoAdmin = await _host.CreateGrupoAsync(ana);
        var grupoDaAna = await _host.CreateGrupoAsync(ana);
        var admin = await _host.AddMembroAsync(grupoDoAdmin, PapelMembro.Administrador);
        var outraAdmin = await _host.AddMembroAsync(grupoDaAna, PapelMembro.Administrador);
        var doAdmin = await CreateConviteAsync(admin, grupoDoAdmin);
        var daAna = await CreateConviteAsync(ana, grupoDaAna);

        await admin.Client.PostAsync($"/api/grupos/{grupoDoAdmin}/sair", null, Token);
        await outraAdmin.Client.PostAsync($"/api/grupos/{grupoDaAna}/sair", null, Token);

        var anonimo = _host.CreateAnonimo();
        Assert.Equal(HttpStatusCode.NotFound, (await ConsultarAsync(anonimo, doAdmin.Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ConsultarAsync(anonimo, daAna.Token)).StatusCode);
    }

    [Fact]
    public async Task Removing_RevokesTheLink_WhoeverCreatedIt_SoTheRemovedMemberCannotReturn()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId);
        var convite = await CreateConviteAsync(ana, grupoId);

        var response = await ana.Client.DeleteAsync($"/api/grupos/{grupoId}/membros/{bia.Id}", Token);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await ApiTestHost.AssertProblemAsync(
            await EntrarAsync(bia.Client, convite.Token),
            HttpStatusCode.NotFound,
            "CONVITE_INVALIDO");
        Assert.Null((await GetConviteAsync(ana, grupoId)).Convite);
    }

    [Fact]
    public async Task RemovedMember_ComesBackOnlyWithANewInvite_AsMembro_KeepingTheFirstEntry()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var admin = await _host.AddMembroAsync(grupoId, PapelMembro.Administrador);
        await _host.ExecuteAsync(
            $"UPDATE membros_grupo SET joined_at = now() - interval '60 days' WHERE usuario_id = '{admin.Id}'");
        await ana.Client.DeleteAsync($"/api/grupos/{grupoId}/membros/{admin.Id}", Token);
        var convite = await CreateConviteAsync(ana, grupoId);

        var response = await EntrarAsync(admin.Client, convite.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grupo = await admin.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}", AppJsonContext.Default.GrupoResponse, Token);
        Assert.Equal(PapelMembro.Membro, grupo!.Papel);
        Assert.Equal(2, grupo.TotalMembros);
        Assert.Equal(1L, await _host.CountAsync($"""
            SELECT count(*) FROM membros_grupo
            WHERE usuario_id = '{admin.Id}' AND joined_at < now() - interval '59 days'
            """));
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
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => cookie.StartsWith("wx_session=", StringComparison.Ordinal));
        Assert.Equal(1L, await _host.CountAsync($"""
            SELECT count(*)
            FROM membros_grupo m
            JOIN usuarios u ON u.id = m.usuario_id
            WHERE u.email = 'nova@exemplo.com' AND m.grupo_id = '{grupoId}' AND m.papel = 'Membro'
            """));
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
    public async Task Registrar_InAFullGroup_LeavesNoAccountBehind()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        await FillGrupoAsync(grupoId, GrupoEntrada.MaxMembros - 1);

        var response = await RegistrarAsync(_host.CreateAnonimo(), "nova@exemplo.com", convite.Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "GRUPO_CHEIO");
        Assert.Equal(0L, await _host.CountAsync("SELECT count(*) FROM usuarios WHERE email = 'nova@exemplo.com'"));
    }

    [Fact]
    public async Task Registrar_WithARepeatedEmail_IsRefused()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var convite = await CreateConviteAsync(ana, grupoId);
        await _host.ExecuteAsync("UPDATE usuarios SET email = 'ana@exemplo.com'");

        var response = await RegistrarAsync(_host.CreateAnonimo(), "ana@exemplo.com", convite.Token);

        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "EMAIL_JA_CADASTRADO");
    }

    private async Task FillGrupoAsync(Guid grupoId, int quantidade)
    {
        for (var i = 0; i < quantidade; i++)
        {
            await _host.AddMembroAsync(grupoId);
        }
    }

    private static async Task<ConviteResponse> CreateConviteAsync(TestUsuario criador, Guid grupoId)
    {
        var response = await criador.Client.PostAsync($"/api/grupos/{grupoId}/convite", null, Token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync(AppJsonContext.Default.ConviteResponse, Token))!;
    }

    private static async Task<ConviteAtualResponse> GetConviteAsync(TestUsuario leitor, Guid grupoId) =>
        (await leitor.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}/convite",
            AppJsonContext.Default.ConviteAtualResponse,
            Token))!;

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
