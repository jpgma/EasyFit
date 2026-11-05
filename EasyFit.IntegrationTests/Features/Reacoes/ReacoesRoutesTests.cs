using System.Net;
using System.Net.Http.Json;
using EasyFit.Features.Atividades;
using EasyFit.Features.Grupos.Contracts;
using EasyFit.Features.Reacoes;
using EasyFit.Infrastructure.Json;
using Situacao = EasyFit.Features.Reacoes.SituacaoVerificacao;

namespace EasyFit.IntegrationTests.Features.Reacoes;

public sealed class ReacoesRoutesTests : IAsyncLifetime
{
    private ApiTestHost _host = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _host = await ApiTestHost.CreateAsync();

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task Create_SemRevisores_VerificadaImediata()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);

        var criada = await PostAtividadeAsync(ana, grupoId);
        var atividade = await criada.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);

        Assert.Equal(Situacao.Verificada, atividade!.SituacaoVerificacao);
    }

    [Fact]
    public async Task PutReacao_AutorRecebe403()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId, PapelMembro.Membro, "Bia");

        var criada = await PostAtividadeAsync(ana, grupoId);
        var atividade = await criada.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);

        var response = await PutReacaoAsync(ana, atividade!.Id, TipoReacao.Kudos);
        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Forbidden, "AUTOR_NAO_REAGE");
    }

    [Fact]
    public async Task PutReacao_TodosKudos_Verificada()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId, PapelMembro.Membro, "Bia");

        var criada = await PostAtividadeAsync(ana, grupoId);
        var atividade = await criada.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);

        var response = await PutReacaoAsync(bia, atividade!.Id, TipoReacao.Kudos);
        response.EnsureSuccessStatusCode();

        var detalhe = await ana.Client.GetAsync($"/api/atividades/{atividade.Id}", Token);
        var atualizada = await detalhe.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);
        Assert.Equal(Situacao.Verificada, atualizada!.SituacaoVerificacao);
    }

    [Fact]
    public async Task PutReacao_Suspeito_EmRevisao()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId, PapelMembro.Membro, "Bia");

        var criada = await PostAtividadeAsync(ana, grupoId);
        var atividade = await criada.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);

        await PutReacaoAsync(bia, atividade!.Id, TipoReacao.Suspeito);

        var detalhe = await ana.Client.GetAsync($"/api/atividades/{atividade.Id}", Token);
        var atualizada = await detalhe.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);
        Assert.Equal(Situacao.EmRevisao, atualizada!.SituacaoVerificacao);
    }

    [Fact]
    public async Task PostRevisao_AdminResolve()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId, PapelMembro.Membro, "Bia");

        var criada = await PostAtividadeAsync(ana, grupoId);
        var atividade = await criada.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);
        await PutReacaoAsync(bia, atividade!.Id, TipoReacao.Suspeito);

        var response = await PostRevisaoAsync(ana, atividade.Id, Situacao.Verificada, "Evidência consistente.");
        response.EnsureSuccessStatusCode();

        var detalhe = await ana.Client.GetAsync($"/api/atividades/{atividade.Id}", Token);
        var atualizada = await detalhe.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);
        Assert.Equal(Situacao.Verificada, atualizada!.SituacaoVerificacao);
    }

    [Fact]
    public async Task Encerramento_PrazoExpirado_Verificada()
    {
        var ana = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(ana);
        var bia = await _host.AddMembroAsync(grupoId, PapelMembro.Membro, "Bia");

        var criada = await PostAtividadeAsync(ana, grupoId);
        var atividade = await criada.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);

        await _host.ExecuteAsync(
            $"UPDATE atividades SET submitted_at = now() - interval '49 hours' WHERE id = '{atividade!.Id}'");

        await VerificacaoAtividade.EncerrarJanelasExpiradasAsync(_host.DataSource, Token);

        var detalhe = await ana.Client.GetAsync($"/api/atividades/{atividade.Id}", Token);
        var atualizada = await detalhe.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);
        Assert.Equal(Situacao.Verificada, atualizada!.SituacaoVerificacao);
    }

    [Fact]
    public async Task PutReacao_EmRevisao_LimiteAtingidoRejeita()
    {
        var (ana, atividadeId, revisores) = await CriarComRevisoresAsync(3);

        (await PutReacaoAsync(revisores[0], atividadeId, TipoReacao.Rejeitar)).EnsureSuccessStatusCode();
        Assert.Equal(Situacao.EmRevisao, await SituacaoAsync(ana, atividadeId));

        var resposta = await PutReacaoAsync(revisores[1], atividadeId, TipoReacao.Rejeitar);
        resposta.EnsureSuccessStatusCode();
        var reacoes = await resposta.Content.ReadFromJsonAsync(AppJsonContext.Default.ReacoesResponse, Token);
        Assert.Equal(2, reacoes!.RejeicoesNecessarias);
        Assert.Equal(Situacao.Rejeitada, await SituacaoAsync(ana, atividadeId));
    }

    [Fact]
    public async Task PutReacao_TrocarUnicaNegativa_VoltaAAguardar()
    {
        var (ana, atividadeId, revisores) = await CriarComRevisoresAsync(2);

        (await PutReacaoAsync(revisores[0], atividadeId, TipoReacao.Suspeito)).EnsureSuccessStatusCode();
        Assert.Equal(Situacao.EmRevisao, await SituacaoAsync(ana, atividadeId));

        (await PutReacaoAsync(revisores[0], atividadeId, TipoReacao.Kudos)).EnsureSuccessStatusCode();
        Assert.Equal(Situacao.AguardandoFeedback, await SituacaoAsync(ana, atividadeId));
    }

    [Fact]
    public async Task PutReacao_EmRevisaoAposPrazo_Recusada()
    {
        var (ana, atividadeId, revisores) = await CriarComRevisoresAsync(2);
        (await PutReacaoAsync(revisores[0], atividadeId, TipoReacao.Suspeito)).EnsureSuccessStatusCode();

        await _host.ExecuteAsync(
            $"UPDATE atividades SET submitted_at = now() - interval '49 hours' WHERE id = '{atividadeId}'");

        var response = await PutReacaoAsync(revisores[1], atividadeId, TipoReacao.Kudos);
        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "JANELA_ENCERRADA");
        Assert.Equal(Situacao.EmRevisao, await SituacaoAsync(ana, atividadeId));
    }

    [Fact]
    public async Task PutReacao_AposDecisaoAdmin_Recusada()
    {
        var (ana, atividadeId, revisores) = await CriarComRevisoresAsync(2);
        (await PutReacaoAsync(revisores[0], atividadeId, TipoReacao.Suspeito)).EnsureSuccessStatusCode();
        (await PostRevisaoAsync(ana, atividadeId, Situacao.Verificada, "Conferido.")).EnsureSuccessStatusCode();

        var response = await PutReacaoAsync(revisores[1], atividadeId, TipoReacao.Rejeitar);
        await ApiTestHost.AssertProblemAsync(response, HttpStatusCode.Conflict, "REACAO_ENCERRADA");
    }

    private async Task<(TestUsuario Dono, Guid AtividadeId, List<TestUsuario> Revisores)>
        CriarComRevisoresAsync(int quantidade)
    {
        var dono = await _host.CreateUsuarioAsync("Ana");
        var grupoId = await _host.CreateGrupoAsync(dono);
        var revisores = new List<TestUsuario>();
        for (var i = 0; i < quantidade; i++)
        {
            revisores.Add(await _host.AddMembroAsync(grupoId, PapelMembro.Membro, $"Revisor {i}"));
        }

        var criada = await PostAtividadeAsync(dono, grupoId);
        var atividade = await criada.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);
        return (dono, atividade!.Id, revisores);
    }

    private static async Task<string> SituacaoAsync(TestUsuario usuario, Guid atividadeId)
    {
        var detalhe = await usuario.Client.GetAsync($"/api/atividades/{atividadeId}", Token);
        var atividade = await detalhe.Content.ReadFromJsonAsync(AppJsonContext.Default.AtividadeResponse, Token);
        return atividade!.SituacaoVerificacao;
    }

    private static CriarAtividadeRequest Valid() => new(
        TipoAtividade.Corrida,
        3600,
        DateTimeOffset.UtcNow.AddMinutes(-30),
        null,
        TipoComprovacao.RegistroManual);

    private static Task<HttpResponseMessage> PostAtividadeAsync(TestUsuario usuario, Guid grupoId) =>
        usuario.Client.PostAsJsonAsync(
            $"/api/grupos/{grupoId}/atividades",
            Valid(),
            AppJsonContext.Default.CriarAtividadeRequest,
            Token);

    private static Task<HttpResponseMessage> PutReacaoAsync(TestUsuario usuario, Guid atividadeId, string tipo) =>
        usuario.Client.PutAsJsonAsync(
            $"/api/atividades/{atividadeId}/reacao",
            new ReacaoRequest(tipo),
            AppJsonContext.Default.ReacaoRequest,
            Token);

    private static Task<HttpResponseMessage> PostRevisaoAsync(
        TestUsuario usuario,
        Guid atividadeId,
        string decisao,
        string justificativa) =>
        usuario.Client.PostAsJsonAsync(
            $"/api/atividades/{atividadeId}/revisao",
            new RevisaoRequest(decisao, justificativa),
            AppJsonContext.Default.RevisaoRequest,
            Token);
}
