using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public sealed class WhatsappConviteCadastroServico(
    IEntregaNotificacaoExternaServico entregaNotificacao,
    IOptions<ConfiguracaoWhatsappConviteCadastro> configuracaoAccessor)
    : IEnvioWhatsappConviteCadastroServico
{
    private readonly ConfiguracaoWhatsappConviteCadastro configuracao = configuracaoAccessor.Value;

    public async Task<ResultadoEnvioWhatsappConviteDto> EnviarAsync(
        ConviteCadastro conviteCadastro,
        string codigoConvite,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conviteCadastro.Telefone))
            return new(false, false, "Telefone do convite não informado para envio por WhatsApp.", null);
        if (string.IsNullOrWhiteSpace(conviteCadastro.WhatsappIdempotencyKey))
            return new(false, false, "A solicitação de WhatsApp não possui chave de idempotência.", null);

        var link = ConteudoConviteCadastro.MontarLinkConvite(
            configuracao.ObterUrlAppBase(), conviteCadastro.IdentificadorPublico);
        var solicitacao = new SolicitacaoEntregaNotificacaoDto(
            configuracao.Source.Trim(),
            conviteCadastro.WhatsappIdempotencyKey,
            CanalNotificacaoExterna.Whatsapp,
            configuracao.TemplateKey.Trim(),
            conviteCadastro.Telefone,
            new Dictionary<string, string>
            {
                ["codigoConvite"] = codigoConvite,
                ["linkConvite"] = link
            });

        var resultado = await entregaNotificacao.EnviarAsync(solicitacao, cancellationToken);
        return new ResultadoEnvioWhatsappConviteDto(
            resultado.TentativaRealizada,
            resultado.Enviado,
            resultado.Erro,
            resultado.IdentificadorMensagem,
            resultado.Aceito);
    }
}
