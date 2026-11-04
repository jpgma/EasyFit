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
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha ao encerrar janelas de reação.");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }
}
