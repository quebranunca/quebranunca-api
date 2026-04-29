using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;
using Resend;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public class ResendEmailConviteCadastroServico(
    HttpClient httpClient,
    IOptions<ConfiguracaoEmailConviteCadastro> configuracaoAccessor,
    ILogger<ResendEmailConviteCadastroServico> logger
) : IEnvioEmailConviteCadastroServico
{
    private readonly ConfiguracaoEmailConviteCadastro configuracao = configuracaoAccessor.Value;

    public async Task<ResultadoEnvioEmailConviteDto> EnviarAsync(
        ConviteCadastro conviteCadastro,
        string codigoConvite,
        CancellationToken cancellationToken = default)
    {
        var mensagemConfiguracaoIncompleta = configuracao.ObterMensagemConfiguracaoIncompleta();
        if (mensagemConfiguracaoIncompleta is not null)
        {
            logger.LogWarning(
                "Envio automático de e-mail para o convite {ConviteId} ignorado. {Mensagem}.",
                conviteCadastro.Id,
                mensagemConfiguracaoIncompleta);
            return new ResultadoEnvioEmailConviteDto(false, false, mensagemConfiguracaoIncompleta, null);
        }

        var linkConvite = ConteudoConviteCadastro.MontarLinkConvite(configuracao.ObterUrlAppBase(), conviteCadastro.IdentificadorPublico);

        try
        {
            var resend = CriarClienteResend();
            var response = await resend.EmailSendAsync(
                $"convite-cadastro-{conviteCadastro.Id:N}-{conviteCadastro.DataAtualizacao.Ticks}",
                CriarMensagem(conviteCadastro, linkConvite, codigoConvite),
                cancellationToken);

            if (!response.Success)
            {
                var erro = ExtrairMensagemErro(response.Exception);
                logger.LogWarning(
                    "Falha ao enviar e-mail automático do convite {ConviteId}. Status {StatusCode}. Erro: {Erro}.",
                    conviteCadastro.Id,
                    response.Exception?.StatusCode is null ? null : (int?)response.Exception.StatusCode,
                    erro);
                return new ResultadoEnvioEmailConviteDto(true, false, erro, null);
            }

            logger.LogInformation(
                "E-mail automático do convite {ConviteId} enviado com sucesso. Mensagem: {MensagemId}.",
                conviteCadastro.Id,
                response.Content);

            return new ResultadoEnvioEmailConviteDto(true, true, null, response.Content.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar e-mail automático do convite {ConviteId}.", conviteCadastro.Id);
            return new ResultadoEnvioEmailConviteDto(true, false, ex.Message, null);
        }
    }

    private IResend CriarClienteResend()
    {
        var options = new ResendClientOptions
        {
            ApiToken = configuracao.ApiKey.Trim(),
            ApiUrl = configuracao.ObterBaseUrl(),
            ThrowExceptions = false
        };

        return ResendClient.Create(options, httpClient);
    }

    private EmailMessage CriarMensagem(ConviteCadastro conviteCadastro, string linkConvite, string codigoConvite)
    {
        var mensagem = new EmailMessage
        {
            From = configuracao.ObterRemetenteFormatado(),
            Subject = ConteudoConviteCadastro.MontarAssuntoEmail(),
            TextBody = ConteudoConviteCadastro.MontarTextoEmail(conviteCadastro, linkConvite, codigoConvite),
            HtmlBody = ConteudoConviteCadastro.MontarHtmlEmail(conviteCadastro, linkConvite, codigoConvite)
        };

        mensagem.To.Add(conviteCadastro.Email);

        if (!string.IsNullOrWhiteSpace(configuracao.ReplyTo))
        {
            mensagem.ReplyTo = configuracao.ReplyTo.Trim();
        }

        return mensagem;
    }

    private static string ExtrairMensagemErro(ResendException? exception)
    {
        if (!string.IsNullOrWhiteSpace(exception?.Message))
        {
            return exception.Message.Length <= 500
                ? exception.Message
                : exception.Message[..500];
        }

        return "Falha ao enviar o e-mail do convite.";
    }
}
