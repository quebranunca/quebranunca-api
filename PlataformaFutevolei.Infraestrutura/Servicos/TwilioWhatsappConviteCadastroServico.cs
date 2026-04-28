using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;
using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public class TwilioWhatsappConviteCadastroServico(
    IOptions<ConfiguracaoWhatsappConviteCadastro> configuracaoAccessor,
    ILogger<TwilioWhatsappConviteCadastroServico> logger
) : IEnvioWhatsappConviteCadastroServico
{
    private readonly ConfiguracaoWhatsappConviteCadastro configuracao = configuracaoAccessor.Value;

    public async Task<ResultadoEnvioWhatsappConviteDto> EnviarAsync(
        ConviteCadastro conviteCadastro,
        string codigoConvite,
        CancellationToken cancellationToken = default)
    {
        var mensagemConfiguracaoIncompleta = configuracao.ObterMensagemConfiguracaoIncompleta();
        if (mensagemConfiguracaoIncompleta is not null)
        {
            logger.LogInformation(
                "Envio automático de WhatsApp para o convite {ConviteId} ignorado. {Mensagem}.",
                conviteCadastro.Id,
                mensagemConfiguracaoIncompleta);
            return new ResultadoEnvioWhatsappConviteDto(false, false, mensagemConfiguracaoIncompleta, null);
        }

        var telefoneDestino = NormalizarTelefoneWhatsapp(conviteCadastro.Telefone);
        if (telefoneDestino is null)
        {
            const string erroTelefone = "Telefone do convite não informado ou inválido para envio por WhatsApp.";
            logger.LogWarning(
                "Envio automático de WhatsApp para o convite {ConviteId} não realizado. {Erro}.",
                conviteCadastro.Id,
                erroTelefone);
            return new ResultadoEnvioWhatsappConviteDto(true, false, erroTelefone, null);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var linkConvite = ConteudoConviteCadastro.MontarLinkConvite(configuracao.ObterUrlAppBase(), conviteCadastro.IdentificadorPublico);
        var remetente = new PhoneNumber(FormatarEnderecoWhatsapp(configuracao.RemetenteWhatsapp));
        var destinatario = new PhoneNumber(FormatarEnderecoWhatsapp(telefoneDestino));
        var clienteTwilio = new TwilioRestClient(configuracao.AccountSid.Trim(), configuracao.AuthToken.Trim());
        var opcoes = new CreateMessageOptions(destinatario)
        {
            PathAccountSid = configuracao.AccountSid.Trim(),
            From = remetente,
            Body = ConteudoConviteCadastro.MontarTextoWhatsapp(conviteCadastro, linkConvite, codigoConvite)
        };

        try
        {
            var mensagem = await MessageResource.CreateAsync(opcoes, clienteTwilio);

            if (mensagem.ErrorCode is not null)
            {
                var erro = string.IsNullOrWhiteSpace(mensagem.ErrorMessage)
                    ? $"Falha ao enviar o WhatsApp do convite. Código Twilio: {mensagem.ErrorCode}."
                    : mensagem.ErrorMessage;
                logger.LogWarning(
                    "Falha ao enviar WhatsApp do convite {ConviteId}. Código Twilio {CodigoErro}. Erro: {Erro}.",
                    conviteCadastro.Id,
                    mensagem.ErrorCode,
                    erro);
                return new ResultadoEnvioWhatsappConviteDto(true, false, erro, mensagem.Sid);
            }

            logger.LogInformation(
                "WhatsApp do convite {ConviteId} enviado com sucesso. Mensagem: {MensagemId}.",
                conviteCadastro.Id,
                mensagem.Sid);

            return new ResultadoEnvioWhatsappConviteDto(true, true, null, mensagem.Sid);
        }
        catch (ApiException ex)
        {
            logger.LogWarning(
                ex,
                "Falha da Twilio ao enviar WhatsApp do convite {ConviteId}. Código {CodigoErro}.",
                conviteCadastro.Id,
                ex.Code);
            return new ResultadoEnvioWhatsappConviteDto(true, false, ex.Message, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar WhatsApp do convite {ConviteId}.", conviteCadastro.Id);
            return new ResultadoEnvioWhatsappConviteDto(true, false, ex.Message, null);
        }
    }

    private static string? NormalizarTelefoneWhatsapp(string? telefone)
    {
        var telefoneNormalizado = new string((telefone ?? string.Empty)
            .Where(c => char.IsDigit(c) || c == '+')
            .ToArray())
            .Trim();

        if (string.IsNullOrWhiteSpace(telefoneNormalizado))
        {
            return null;
        }

        if (!telefoneNormalizado.StartsWith('+'))
        {
            telefoneNormalizado = telefoneNormalizado.StartsWith("55", StringComparison.Ordinal)
                ? $"+{telefoneNormalizado}"
                : telefoneNormalizado.Length is 10 or 11
                    ? $"+55{telefoneNormalizado}"
                    : $"+{telefoneNormalizado}";
        }

        return telefoneNormalizado.Length < 12 ? null : telefoneNormalizado;
    }

    private static string FormatarEnderecoWhatsapp(string telefone)
    {
        var telefoneNormalizado = telefone.Trim();
        return telefoneNormalizado.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase)
            ? telefoneNormalizado
            : $"whatsapp:{telefoneNormalizado}";
    }
}
