using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Api.Inicializacao;

internal static class InicializacaoMassaTesteAi
{
    public static async Task PrepararAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        var configuracao = app.Configuration
            .GetSection(MassaTesteAiConfiguracao.Secao)
            .Get<MassaTesteAiConfiguracao>() ?? new MassaTesteAiConfiguracao();

        if (!configuracao.Habilitada)
        {
            app.Logger.LogInformation("Massa [AI TESTE] desabilitada por configuração.");
            return;
        }

        if (app.Environment.IsProduction())
        {
            throw new InvalidOperationException("Massa [AI TESTE] não pode ser criada automaticamente em Production.");
        }

        await using var scope = app.Services.CreateAsyncScope();
        var servico = scope.ServiceProvider.GetRequiredService<IMassaTesteAiServico>();
        var resultado = await servico.GarantirAsync(configuracao, cancellationToken);

        app.Logger.LogInformation(
            "Massa [AI TESTE] preparada. UsuarioId={UsuarioId}; AtletaUsuarioId={AtletaUsuarioId}; GrupoId={GrupoId}; ArenaId={ArenaId}; AtletasAuxiliares={AtletasAuxiliares}; VinculosGrupo={VinculosGrupo}.",
            resultado.UsuarioId,
            resultado.AtletaUsuarioId,
            resultado.GrupoId,
            resultado.ArenaId,
            resultado.AtletasAuxiliares,
            resultado.VinculosGrupo);
    }
}
