using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public class GeracaoLinkConviteCadastroServico(
    IOptions<ConfiguracaoEmailConviteCadastro> configuracaoAccessor
) : IGeracaoLinkConviteCadastroServico
{
    private readonly ConfiguracaoEmailConviteCadastro configuracao = configuracaoAccessor.Value;

    public string Gerar(ConviteCadastro conviteCadastro)
    {
        var mensagemConfiguracaoIncompleta = configuracao.ObterMensagemConfiguracaoIncompleta();
        if (mensagemConfiguracaoIncompleta is not null)
        {
            throw new InvalidOperationException(mensagemConfiguracaoIncompleta);
        }

        return ConteudoConviteCadastro.MontarLinkConvite(
            configuracao.ObterUrlAppBase(),
            conviteCadastro.IdentificadorPublico);
    }
}
