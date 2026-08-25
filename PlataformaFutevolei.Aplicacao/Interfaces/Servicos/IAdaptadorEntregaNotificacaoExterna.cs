using PlataformaFutevolei.Aplicacao.DTOs;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface IAdaptadorEntregaNotificacaoExterna
{
    CanalNotificacaoExterna Canal { get; }

    Task<ResultadoEntregaNotificacaoDto> EnviarAsync(
        SolicitacaoEntregaNotificacaoDto solicitacao,
        CancellationToken cancellationToken = default);
}
