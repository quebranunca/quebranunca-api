using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public class SaneamentoAtletasEmailServico(
    IConsolidacaoAtletaServico consolidacaoAtletaServico
) : ISaneamentoAtletasEmailServico
{
    public Task<SaneamentoAtletasEmailResumoDto> UnificarDuplicadosPorEmailAsync(
        CancellationToken cancellationToken = default)
    {
        return consolidacaoAtletaServico.ConsolidarDuplicadosPorEmailAsync(cancellationToken);
    }
}
