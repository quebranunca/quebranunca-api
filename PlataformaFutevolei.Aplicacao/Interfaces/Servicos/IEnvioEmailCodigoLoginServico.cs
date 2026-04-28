using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface IEnvioEmailCodigoLoginServico
{
    Task<ResultadoEnvioEmailCodigoLoginDto> EnviarAsync(
        Usuario usuario,
        string codigo,
        CancellationToken cancellationToken = default);
}
