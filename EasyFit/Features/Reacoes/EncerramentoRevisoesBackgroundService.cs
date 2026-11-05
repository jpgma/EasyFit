using EasyFit.Features.Notificacoes.Contracts;

namespace EasyFit.Features.Reacoes;

internal sealed class EncerramentoRevisoesBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EncerramentoRevisoesBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dataSource = scope.ServiceProvider.GetRequiredService<Npgsql.NpgsqlDataSource>();
                var alteradas = await VerificacaoAtividade.EncerrarJanelasExpiradasAsync(dataSource, stoppingToken);
                if (alteradas > 0)
                {
                    logger.LogInformation("Encerradas janelas de reação em {Quantidade} atividades.", alteradas);
                }

                await NotificacoesAtividade.ExcluirAntigasAsync(dataSource, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha na manutenção periódica de revisões e notificações.");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }
}
