using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface IGeracaoLinkConviteCadastroServico
{
    string Gerar(ConviteCadastro conviteCadastro);
}
