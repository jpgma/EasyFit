using System.Net;
using System.Net.Http.Json;
using EasyFit.Features.Desafios;
using EasyFit.Features.Grupos.Contracts;
using EasyFit.Infrastructure.Json;

namespace EasyFit.IntegrationTests.Features.Desafios;

public sealed class DesafiosRoutesTests : IAsyncLifetime
{
    private const string HojeNoGrupo = "(now() AT TIME ZONE 'America/Sao_Paulo')::date";
    private const string SegundaDaSemana = $"date_trunc('week', {HojeNoGrupo})::date";

    private ApiTestHost _host = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // Com dois dias de folga, a data continua no futuro em qualquer fuso.
    private static DateOnly ProximaSegunda
    {
        get
        {
            var dia = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);
            while (dia.DayOfWeek != DayOfWeek.Monday)
            {
                dia = dia.AddDays(1);
            }

            return dia;
        }
    }

    public async ValueTask InitializeAsync() => _host = await ApiTestHost.CreateAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task Create_ReturnsTheDesafio_WithTheCreatorAsParticipante()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var inicio = ProximaSegunda;

        var response = await PostDesafioAsync(ana, grupoId, Valid() with
        {
            Nome = "  Consistência  ",
            Descricao = "  Bora  ",
            DataInicio = inicio,
            DataFim = inicio.AddDays(27),
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var desafio = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.DesafioResponse, Token);
        Assert.Equal(
            new DesafioResponse(
                desafio!.Id,
                grupoId,
                "Consistência",
                "Bora",
                inicio,
                inicio.AddDays(27),
                3600,
                4,
                SituacaoDesafio.Agendado,
                "America/Sao_Paulo",
                PapelMembro.Proprietario,
                Participando: true,
                TotalParticipantes: 1),
            desafio);
    }

    [Fact]
    public async Task Create_AcceptsAMissingDescricao()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);

        var response = await PostDesafioAsync(ana, grupoId, Valid() with { Descricao = "   " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var desafio = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.DesafioResponse, Token);
        Assert.Null(desafio!.Descricao);
    }

    [Fact]
    public async Task Create_RejectsInvalidData()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var inicio = ProximaSegunda;

        CriarDesafioRequest[] invalidos =
        [
            Valid() with { Nome = " " },
            Valid() with { Descricao = new string('a', 501) },
            Valid() with { DuracaoMinimaSegundos = 0 },
            Valid() with { DuracaoMinimaSegundos = null },
            Valid() with { SessoesPorSemana = 8 },
            Valid() with { SessoesPorSemana = null },
            Valid() with { DataInicio = null },
            Valid() with { DataFim = null },
            Valid() with { DataInicio = inicio.AddDays(1) },
            Valid() with { DataFim = inicio.AddDays(5) },
            Valid() with { DataInicio = inicio.AddDays(-28), DataFim = inicio.AddDays(-22) },
        ];

        foreach (var invalido in invalidos)
        {
            await ApiTestHost.AssertProblemAsync(
                await PostDesafioAsync(ana, grupoId, invalido),
                HttpStatusCode.BadRequest,
                "DADOS_INVALIDOS");
        }

        Assert.Equal(0L, await _host.CountAsync("SELECT count(*) FROM desafios"));
    }

    [Fact]
    public async Task Create_RequiresProprietarioOrAdministrador()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var administradora = await _host.AddMembroAsync(grupoId, PapelMembro.Administrador);
        var membro = await _host.AddMembroAsync(grupoId);

        Assert.Equal(HttpStatusCode.Created, (await PostDesafioAsync(administradora, grupoId, Valid())).StatusCode);
        await ApiTestHost.AssertProblemAsync(
            await PostDesafioAsync(membro, grupoId, Valid()),
            HttpStatusCode.Forbidden,
            "ACESSO_NEGADO");
    }

    [Fact]
    public async Task Create_AllowsConcurrentDesafios_InTheSameGrupo()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);

        await CreateDesafioAsync(ana, grupoId, "Um");
        await CreateDesafioAsync(ana, grupoId, "Dois");

        Assert.Equal(2, (await ListAsync(ana, grupoId)).Count);
    }

    [Fact]
    public async Task EveryEndpoint_RequiresASession()
    {
        var anonimo = _host.CreateAnonimo();

        foreach (var (method, path) in AllEndpoints(Guid.NewGuid(), Guid.NewGuid()))
        {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(new { }) };
            var response = await anonimo.SendAsync(request, Token);
            await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Unauthorized, "NAO_AUTENTICADO");
        }
    }

    [Fact]
    public async Task EveryEndpoint_HidesTheDesafioFromOutsiders_AndRemovedMembers()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var desafioId = await CreateDesafioAsync(ana, grupoId);
        var estranho = await _host.CreateUsuarioAsync();
        var removida = await _host.AddMembroAsync(grupoId);
        await RemoverAsync(ana, grupoId, removida);

        foreach (var usuario in new[] { estranho, removida })
        {
            await ApiTestHost.AssertProblemAsync(
                await usuario.Client.GetAsync($"/api/grupos/{grupoId}/desafios", Token),
                HttpStatusCode.NotFound,
                "GRUPO_NAO_ENCONTRADO");
            await ApiTestHost.AssertProblemAsync(
                await PostDesafioAsync(usuario, grupoId, Valid()),
                HttpStatusCode.NotFound,
                "GRUPO_NAO_ENCONTRADO");
            await ApiTestHost.AssertProblemAsync(
                await usuario.Client.GetAsync($"/api/desafios/{desafioId}", Token),
                HttpStatusCode.NotFound,
                "DESAFIO_NAO_ENCONTRADO");
            await ApiTestHost.AssertProblemAsync(
                await usuario.Client.GetAsync($"/api/desafios/{desafioId}/progresso", Token),
                HttpStatusCode.NotFound,
                "DESAFIO_NAO_ENCONTRADO");
            await ApiTestHost.AssertProblemAsync(
                await ParticiparAsync(usuario, desafioId),
                HttpStatusCode.NotFound,
                "DESAFIO_NAO_ENCONTRADO");
            await ApiTestHost.AssertProblemAsync(
                await CancelarAsync(usuario, desafioId),
                HttpStatusCode.NotFound,
                "DESAFIO_NAO_ENCONTRADO");
        }

        Assert.Equal(1L, await _host.CountAsync("SELECT count(*) FROM participantes_desafio"));
        Assert.Equal(0L, await _host.CountAsync("SELECT count(*) FROM desafios WHERE cancelled_at IS NOT NULL"));
    }

    [Fact]
    public async Task NonexistentDesafio_RespondsLikeAHiddenOne()
    {
        var ana = await _host.CreateUsuarioAsync();

        await ApiTestHost.AssertProblemAsync(
            await ana.Client.GetAsync($"/api/desafios/{Guid.NewGuid()}", Token),
            HttpStatusCode.NotFound,
            "DESAFIO_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task List_OnlyShowsTheDesafiosOfThatGrupo_WithActiveOnesFirst()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var outroGrupoId = await _host.CreateGrupoAsync(ana);
        var cancelado = await CreateDesafioAsync(ana, grupoId, "Cancelado");
        var encerrado = await CreateDesafioAsync(ana, grupoId, "Encerrado");
        var agendado = await CreateDesafioAsync(ana, grupoId, "Agendado");
        await CreateDesafioAsync(ana, outroGrupoId, "De outro grupo");
        Assert.Equal(HttpStatusCode.NoContent, (await CancelarAsync(ana, cancelado)).StatusCode);
        await EncerrarAsync(encerrado);

        var desafios = await ListAsync(ana, grupoId);

        Assert.Equal([agendado, encerrado, cancelado], desafios.Select(d => d.Id));
        Assert.Equal(
            [SituacaoDesafio.Agendado, SituacaoDesafio.Encerrado, SituacaoDesafio.Cancelado],
            desafios.Select(d => d.Situacao));
    }

    [Fact]
    public async Task Participar_AddsTheMember_AndIsIdempotent()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var desafioId = await CreateDesafioAsync(ana, grupoId);
        var bia = await _host.AddMembroAsync(grupoId);

        Assert.False((await GetAsync(bia, desafioId)).Participando);

        Assert.Equal(HttpStatusCode.NoContent, (await ParticiparAsync(bia, desafioId)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ParticiparAsync(bia, desafioId)).StatusCode);

        var desafio = await GetAsync(bia, desafioId);
        Assert.True(desafio.Participando);
        Assert.Equal(2, desafio.TotalParticipantes);
    }

    [Fact]
    public async Task Participar_AcceptsAnActiveDesafio()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var desafioId = await CreateDesafioAsync(ana, grupoId);
        var bia = await _host.AddMembroAsync(grupoId);
        await IniciarAsync(desafioId);

        Assert.Equal(HttpStatusCode.NoContent, (await ParticiparAsync(bia, desafioId)).StatusCode);
    }

    [Fact]
    public async Task Participar_RejectsEncerrado_AndCancelado()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId);
        var encerrado = await CreateDesafioAsync(ana, grupoId);
        var cancelado = await CreateDesafioAsync(ana, grupoId);
        await EncerrarAsync(encerrado);
        Assert.Equal(HttpStatusCode.NoContent, (await CancelarAsync(ana, cancelado)).StatusCode);

        await ApiTestHost.AssertProblemAsync(
            await ParticiparAsync(bia, encerrado),
            HttpStatusCode.Conflict,
            "DESAFIO_ENCERRADO");
        await ApiTestHost.AssertProblemAsync(
            await ParticiparAsync(bia, cancelado),
            HttpStatusCode.Conflict,
            "DESAFIO_CANCELADO");
    }

    [Fact]
    public async Task LeavingTheGrupo_HidesTheParticipante_AndComingBackRestoresIt()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var desafioId = await CreateDesafioAsync(ana, grupoId);
        var bia = await _host.AddMembroAsync(grupoId, nome: "Bia");
        Assert.Equal(HttpStatusCode.NoContent, (await ParticiparAsync(bia, desafioId)).StatusCode);

        await RemoverAsync(ana, grupoId, bia);

        Assert.Equal(1, (await GetAsync(ana, desafioId)).TotalParticipantes);
        Assert.Equal(["Ana"], (await GetProgressoAsync(ana, desafioId)).Participantes.Select(p => p.NomeExibicao));
        Assert.Equal(2L, await _host.CountAsync("SELECT count(*) FROM participantes_desafio"));

        await _host.ExecuteAsync($"UPDATE membros_grupo SET removed_at = NULL WHERE usuario_id = '{bia.Id}'");

        Assert.Equal(2, (await GetAsync(ana, desafioId)).TotalParticipantes);
        Assert.True((await GetAsync(bia, desafioId)).Participando);
    }

    [Fact]
    public async Task Cancelar_RequiresProprietarioOrAdministrador_AndPreservesTheDesafio()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var administradora = await _host.AddMembroAsync(grupoId, PapelMembro.Administrador);
        var membro = await _host.AddMembroAsync(grupoId);
        var desafioId = await CreateDesafioAsync(ana, grupoId);

        await ApiTestHost.AssertProblemAsync(
            await CancelarAsync(membro, desafioId),
            HttpStatusCode.Forbidden,
            "ACESSO_NEGADO");
        Assert.Equal(SituacaoDesafio.Agendado, (await GetAsync(ana, desafioId)).Situacao);

        Assert.Equal(HttpStatusCode.NoContent, (await CancelarAsync(administradora, desafioId)).StatusCode);

        var desafio = await GetAsync(membro, desafioId);
        Assert.Equal(SituacaoDesafio.Cancelado, desafio.Situacao);
        Assert.Equal(1, desafio.TotalParticipantes);
    }

    [Fact]
    public async Task Cancelar_AcceptsAnActiveDesafio_ButNotAFinishedOne()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var ativo = await CreateDesafioAsync(ana, grupoId);
        var encerrado = await CreateDesafioAsync(ana, grupoId);
        await IniciarAsync(ativo);
        await EncerrarAsync(encerrado);

        Assert.Equal(HttpStatusCode.NoContent, (await CancelarAsync(ana, ativo)).StatusCode);
        await ApiTestHost.AssertProblemAsync(
            await CancelarAsync(ana, ativo),
            HttpStatusCode.Conflict,
            "DESAFIO_CANCELADO");
        await ApiTestHost.AssertProblemAsync(
            await CancelarAsync(ana, encerrado),
            HttpStatusCode.Conflict,
            "DESAFIO_ENCERRADO");
    }

    [Fact]
    public async Task Cancelar_Simultaneo_OnlyOneSucceeds()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var desafioId = await CreateDesafioAsync(ana, grupoId);

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => CancelarAsync(ana, desafioId)));

        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Equal(4, respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Progresso_ShowsTheCurrentWeek_AndOnlyParticipantes()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId, nome: "Bia");
        await _host.AddMembroAsync(grupoId, nome: "Fora do desafio");
        var desafioId = await CreateDesafioAsync(ana, grupoId);
        Assert.Equal(HttpStatusCode.NoContent, (await ParticiparAsync(bia, desafioId)).StatusCode);
        await IniciarAsync(desafioId);

        var progresso = await GetProgressoAsync(ana, desafioId);

        Assert.Equal(4, progresso.SessoesPorSemana);
        Assert.Equal(4, progresso.TotalSemanas);
        Assert.Equal(1, progresso.SemanaAtual?.Numero);
        Assert.Equal(DayOfWeek.Monday, progresso.SemanaAtual?.Inicio.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, progresso.SemanaAtual?.Fim.DayOfWeek);
        Assert.Equal(["Ana", "Bia"], progresso.Participantes.Select(p => p.NomeExibicao));
        Assert.All(progresso.Participantes, p =>
        {
            Assert.Equal(0, p.SessoesVerificadas);
            Assert.Equal(0, p.SessoesProvisorias);
            Assert.False(p.MetaCumprida);
        });
    }

    [Fact]
    public async Task Progresso_HasNoCurrentWeek_UnlessTheDesafioIsRunning()
    {
        var ana = await _host.CreateUsuarioAsync();
        var grupoId = await _host.CreateGrupoAsync(ana);
        var agendado = await CreateDesafioAsync(ana, grupoId);
        var encerrado = await CreateDesafioAsync(ana, grupoId);
        var cancelado = await CreateDesafioAsync(ana, grupoId);
        await EncerrarAsync(encerrado);
        await IniciarAsync(cancelado);
        Assert.Equal(HttpStatusCode.NoContent, (await CancelarAsync(ana, cancelado)).StatusCode);

        foreach (var desafioId in new[] { agendado, encerrado, cancelado })
        {
            Assert.Null((await GetProgressoAsync(ana, desafioId)).SemanaAtual);
        }
    }

    private static CriarDesafioRequest Valid(string nome = "Desafio")
    {
        var inicio = ProximaSegunda;
        return new CriarDesafioRequest(nome, null, inicio, inicio.AddDays(27), 3600, 4);
    }

    private static IEnumerable<(HttpMethod Method, string Path)> AllEndpoints(Guid grupoId, Guid desafioId) =>
    [
        (HttpMethod.Get, $"/api/grupos/{grupoId}/desafios"),
        (HttpMethod.Post, $"/api/grupos/{grupoId}/desafios"),
        (HttpMethod.Get, $"/api/desafios/{desafioId}"),
        (HttpMethod.Get, $"/api/desafios/{desafioId}/progresso"),
        (HttpMethod.Post, $"/api/desafios/{desafioId}/participar"),
        (HttpMethod.Post, $"/api/desafios/{desafioId}/cancelar"),
    ];

    private static Task<HttpResponseMessage> PostDesafioAsync(TestUsuario ator, Guid grupoId, CriarDesafioRequest request) =>
        ator.Client.PostAsJsonAsync(
            $"/api/grupos/{grupoId}/desafios",
            request,
            AppJsonContext.Default.CriarDesafioRequest,
            Token);

    private static async Task<Guid> CreateDesafioAsync(TestUsuario ator, Guid grupoId, string nome = "Desafio")
    {
        var response = await PostDesafioAsync(ator, grupoId, Valid(nome));
        response.EnsureSuccessStatusCode();
        var desafio = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.DesafioResponse, Token);
        return desafio!.Id;
    }

    private static Task<HttpResponseMessage> ParticiparAsync(TestUsuario ator, Guid desafioId) =>
        ator.Client.PostAsync($"/api/desafios/{desafioId}/participar", null, Token);

    private static Task<HttpResponseMessage> CancelarAsync(TestUsuario ator, Guid desafioId) =>
        ator.Client.PostAsync($"/api/desafios/{desafioId}/cancelar", null, Token);

    private static async Task<DesafioResponse> GetAsync(TestUsuario leitor, Guid desafioId) =>
        (await leitor.Client.GetFromJsonAsync(
            $"/api/desafios/{desafioId}",
            AppJsonContext.Default.DesafioResponse,
            Token))!;

    private static async Task<ProgressoResponse> GetProgressoAsync(TestUsuario leitor, Guid desafioId) =>
        (await leitor.Client.GetFromJsonAsync(
            $"/api/desafios/{desafioId}/progresso",
            AppJsonContext.Default.ProgressoResponse,
            Token))!;

    private static async Task<IReadOnlyList<DesafioResponse>> ListAsync(TestUsuario leitor, Guid grupoId) =>
        (await leitor.Client.GetFromJsonAsync(
            $"/api/grupos/{grupoId}/desafios",
            AppJsonContext.Default.DesafiosResponse,
            Token))!.Desafios;

    private static async Task RemoverAsync(TestUsuario ator, Guid grupoId, TestUsuario alvo)
    {
        var response = await ator.Client.DeleteAsync($"/api/grupos/{grupoId}/membros/{alvo.Id}", Token);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // A API não aceita datas no passado: os testes movem o calendário direto no banco.
    private Task IniciarAsync(Guid desafioId) => _host.ExecuteAsync($"""
        UPDATE desafios
        SET data_inicio = {SegundaDaSemana}, data_fim = {SegundaDaSemana} + 27
        WHERE id = '{desafioId}'
        """);

    private Task EncerrarAsync(Guid desafioId) => _host.ExecuteAsync($"""
        UPDATE desafios
        SET data_inicio = {SegundaDaSemana} - 14, data_fim = {SegundaDaSemana} - 8
        WHERE id = '{desafioId}'
        """);
}
