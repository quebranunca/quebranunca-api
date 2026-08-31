using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Servicos;

public sealed class AgendaPresencaGruposWorker(
    IServiceScopeFactory scopeFactory,
    AgendaPresencaGrupoConfiguracao configuracao,
    ILogger<AgendaPresencaGruposWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromMinutes(Math.Clamp(
            configuracao.IntervaloProcessamentoMinutos,
            5,
            120));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var servico = scope.ServiceProvider.GetRequiredService<IPresencaGrupoServico>();
                await servico.ProcessarAgendaDoDiaAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha no processamento periódico da agenda de presença dos grupos.");
            }

            try
            {
                await Task.Delay(intervalo, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
