using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface IEnvioWhatsappConviteCadastroServico
{
    Task<ResultadoEnvioWhatsappConviteDto> EnviarAsync(
        ConviteCadastro conviteCadastro,
        string codigoConvite,
        CancellationToken cancellationToken = default);
}
