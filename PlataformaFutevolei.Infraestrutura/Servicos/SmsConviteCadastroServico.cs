using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public sealed class SmsConviteCadastroServico(
    IEntregaNotificacaoExternaServico entregaNotificacao,
    IOptions<ConfiguracaoSmsZenvia> configuracaoAccessor) : IEnvioSmsConviteCadastroServico
{
    private readonly ConfiguracaoSmsZenvia configuracao = configuracaoAccessor.Value;

    public async Task<ResultadoEnvioSmsConviteDto> EnviarAsync(
        ConviteCadastro conviteCadastro, string codigoConvite, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conviteCadastro.Telefone))
            return new(false, false, "Telefone do convite não informado para envio por SMS.", null);
        if (string.IsNullOrWhiteSpace(conviteCadastro.SmsIdempotencyKey))
            return new(false, false, "A solicitação de SMS não possui chave de idempotência.", null);

        var link = ConteudoConviteCadastro.MontarLinkConvite(
            configuracao.UrlApp.Trim().TrimEnd('/'), conviteCadastro.IdentificadorPublico);
        var texto = $"QuebraNunca: convite {codigoConvite}. Acesse {link}";
        var resultado = await entregaNotificacao.EnviarAsync(new SolicitacaoEntregaNotificacaoDto(
            "quebra-nunca", conviteCadastro.SmsIdempotencyKey, CanalNotificacaoExterna.Sms,
            configuracao.TemplateKey, conviteCadastro.Telefone,
            new Dictionary<string, string> { ["texto"] = texto }), cancellationToken);

        return new(resultado.TentativaRealizada, resultado.Enviado, resultado.Erro,
            resultado.IdentificadorMensagem, resultado.Aceito);
    }
}
