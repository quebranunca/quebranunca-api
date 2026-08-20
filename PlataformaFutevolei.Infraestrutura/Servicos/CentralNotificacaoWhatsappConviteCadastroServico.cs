using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public sealed class CentralNotificacaoWhatsappConviteCadastroServico(
    HttpClient httpClient,
    IOptions<ConfiguracaoWhatsappConviteCadastro> configuracaoAccessor,
    ILogger<CentralNotificacaoWhatsappConviteCadastroServico> logger)
    : IEnvioWhatsappConviteCadastroServico
{
    private readonly ConfiguracaoWhatsappConviteCadastro configuracao = configuracaoAccessor.Value;

    public async Task<ResultadoEnvioWhatsappConviteDto> EnviarAsync(
        ConviteCadastro conviteCadastro,
        string codigoConvite,
        CancellationToken cancellationToken = default)
    {
        var erroConfiguracao = configuracao.ObterMensagemConfiguracaoIncompleta();
        if (erroConfiguracao is not null)
            return new ResultadoEnvioWhatsappConviteDto(false, false, erroConfiguracao, null);
        if (string.IsNullOrWhiteSpace(conviteCadastro.Telefone))
            return new ResultadoEnvioWhatsappConviteDto(true, false,
                "Telefone do convite não informado para envio por WhatsApp.", null);
        if (string.IsNullOrWhiteSpace(conviteCadastro.WhatsappIdempotencyKey))
            return new ResultadoEnvioWhatsappConviteDto(true, false,
                "A solicitação de WhatsApp não possui chave de idempotência.", null);

        var link = ConteudoConviteCadastro.MontarLinkConvite(
            configuracao.ObterUrlAppBase(), conviteCadastro.IdentificadorPublico);
        var request = new
        {
            source = configuracao.Source.Trim(),
            channel = "Whatsapp",
            templateKey = configuracao.TemplateKey.Trim(),
            idempotencyKey = conviteCadastro.WhatsappIdempotencyKey,
            recipientPhone = conviteCadastro.Telefone,
            payload = new
            {
                codigoConvite,
                linkConvite = link
            }
        };

        try
        {
            using var createResponse = await httpClient.PostAsJsonAsync(
                "api/v1/notifications", request, cancellationToken);
            if (!createResponse.IsSuccessStatusCode)
                return await FailureAsync(createResponse, cancellationToken);

            var notification = await createResponse.Content.ReadFromJsonAsync<CentralNotificacaoNotification>(cancellationToken);
            if (notification is null || string.IsNullOrWhiteSpace(notification.Id))
                return new ResultadoEnvioWhatsappConviteDto(true, false,
                    "A Central de Notificações não devolveu o identificador da notificação.", null);

            if (string.Equals(notification.Status, "Sent", StringComparison.OrdinalIgnoreCase))
                return Accepted(notification.Id, sent: true);

            using var processResponse = await httpClient.PostAsync(
                $"api/v1/notifications/{Uri.EscapeDataString(notification.Id)}/process", null, cancellationToken);
            if (processResponse.IsSuccessStatusCode)
            {
                var processed = await processResponse.Content.ReadFromJsonAsync<CentralNotificacaoNotification>(cancellationToken);
                return Accepted(notification.Id,
                    sent: string.Equals(processed?.Status, "Sent", StringComparison.OrdinalIgnoreCase));
            }

            if (processResponse.StatusCode == HttpStatusCode.Conflict)
                return Accepted(notification.Id, sent: false);

            return await FailureAsync(processResponse, cancellationToken, notification.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Falha ao solicitar WhatsApp do convite {ConviteId} à Central de Notificações.", conviteCadastro.Id);
            return new ResultadoEnvioWhatsappConviteDto(true, false,
                "Não foi possível comunicar com o serviço de notificações.", null);
        }
    }

    private static ResultadoEnvioWhatsappConviteDto Accepted(string id, bool sent) =>
        new(true, sent, null, id, Aceito: true);

    private static async Task<ResultadoEnvioWhatsappConviteDto> FailureAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken,
        string? notificationId = null)
    {
        var detail = await ReadProblemDetailAsync(response, cancellationToken);
        return new ResultadoEnvioWhatsappConviteDto(true, false,
            detail ?? $"A Central de Notificações recusou a solicitação ({(int)response.StatusCode}).",
            notificationId);
    }

    private static async Task<string?> ReadProblemDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return document.RootElement.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record CentralNotificacaoNotification(string Id, string Status);
}
