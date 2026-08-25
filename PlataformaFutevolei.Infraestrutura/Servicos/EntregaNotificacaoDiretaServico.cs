using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public sealed class EntregaNotificacaoDiretaServico(
    IEnumerable<IAdaptadorEntregaNotificacaoExterna> adaptadores) : IEntregaNotificacaoExternaServico
{
    public Task<ResultadoEntregaNotificacaoDto> EnviarAsync(
        SolicitacaoEntregaNotificacaoDto solicitacao,
        CancellationToken cancellationToken = default)
    {
        var adaptador = adaptadores.SingleOrDefault(x => x.Canal == solicitacao.Canal);
        return adaptador is null
            ? Task.FromResult(new ResultadoEntregaNotificacaoDto(false, false,
                $"O canal {solicitacao.Canal} ainda não possui um adaptador direto.", null))
            : adaptador.EnviarAsync(solicitacao, cancellationToken);
    }
}
