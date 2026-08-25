using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public sealed class AdaptadorSmsZenviaServico(
    HttpClient httpClient,
    IOptions<ConfiguracaoSmsZenvia> configuracaoAccessor,
    ILogger<AdaptadorSmsZenviaServico> logger) : IAdaptadorEntregaNotificacaoExterna
{
    private readonly ConfiguracaoSmsZenvia configuracao = configuracaoAccessor.Value;
    public CanalNotificacaoExterna Canal => CanalNotificacaoExterna.Sms;

    public async Task<ResultadoEntregaNotificacaoDto> EnviarAsync(
        SolicitacaoEntregaNotificacaoDto solicitacao, CancellationToken cancellationToken = default)
    {
        var erroConfiguracao = configuracao.ObterMensagemConfiguracaoIncompleta();
        if (erroConfiguracao is not null) return Falha(false, erroConfiguracao);
        if (!solicitacao.Dados.TryGetValue("texto", out var texto) || string.IsNullOrWhiteSpace(texto))
            return Falha(false, "O texto da mensagem SMS não foi informado.");

        var telefone = AdaptadorWhatsappWhatsMiauServico.NormalizarTelefoneBrasileiro(solicitacao.Destinatario);
        if (telefone is null) return Falha(false, "O telefone informado não é válido para envio por SMS.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "channels/sms/messages")
            {
                Content = JsonContent.Create(new
                {
                    from = configuracao.Remetente.Trim(),
                    to = telefone,
                    contents = new[] { new { type = "text", text = texto.Trim() } }
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracao.ApiToken.Trim());
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Falha(true, $"A Zenvia recusou a solicitação de SMS ({(int)response.StatusCode}).");

            var identificador = await ObterIdentificadorAsync(response, cancellationToken);
            if (string.IsNullOrWhiteSpace(identificador))
                return Falha(true, "A Zenvia não devolveu o identificador da mensagem SMS.");

            return new ResultadoEntregaNotificacaoDto(true, false, null, identificador, Aceito: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Falha na solicitação de SMS da origem {Origem}.", solicitacao.Origem);
            return Falha(true, "Não foi possível comunicar com a Zenvia.");
        }
    }

    private static ResultadoEntregaNotificacaoDto Falha(bool tentativa, string erro) => new(tentativa, false, erro, null);

    private static async Task<string?> ObterIdentificadorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return document.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}
