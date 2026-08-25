using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public sealed class EntregaNotificacaoDiretaServico(
    HttpClient httpClient,
    IOptions<ConfiguracaoWhatsappConviteCadastro> configuracaoAccessor,
    ILogger<EntregaNotificacaoDiretaServico> logger) : IEntregaNotificacaoExternaServico
{
    private readonly ConfiguracaoWhatsappConviteCadastro configuracao = configuracaoAccessor.Value;

    public async Task<ResultadoEntregaNotificacaoDto> EnviarAsync(
        SolicitacaoEntregaNotificacaoDto solicitacao,
        CancellationToken cancellationToken = default)
    {
        if (solicitacao.Canal != CanalNotificacaoExterna.Whatsapp)
            return Falha(false, $"O canal {solicitacao.Canal} ainda não possui um adaptador direto.");

        var erroConfiguracao = configuracao.ObterMensagemConfiguracaoIncompleta();
        if (erroConfiguracao is not null)
            return Falha(false, erroConfiguracao);

        if (!string.Equals(solicitacao.TemplateKey, "qnf.convite.cadastro.v1", StringComparison.Ordinal))
            return Falha(false, $"O template '{solicitacao.TemplateKey}' não está registrado no módulo de notificações.");

        if (!solicitacao.Dados.TryGetValue("codigoConvite", out var codigo) ||
            !solicitacao.Dados.TryGetValue("linkConvite", out var link))
            return Falha(false, "Os dados obrigatórios do template de convite não foram informados.");

        var telefone = NormalizarTelefoneBrasileiro(solicitacao.Destinatario);
        if (telefone is null)
            return Falha(false, "O telefone informado não é válido para envio por WhatsApp.");

        var mensagem = $"Você recebeu um convite para a Plataforma QuebraNunca Futevôlei.\n\nCódigo: {codigo}\nAcesse: {link}\n\nSe você não esperava esta mensagem, ignore.";
        var endpoint = $"message/sendText/{Uri.EscapeDataString(configuracao.ProvedorInstancia.Trim())}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new { number = telefone, text = mensagem })
            };
            request.Headers.Add("apikey", configuracao.ProvedorApiKey.Trim());

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Falha(true, $"O provedor de WhatsApp recusou a solicitação ({(int)response.StatusCode}).");

            var identificador = await ObterIdentificadorAsync(response, cancellationToken);
            return new ResultadoEntregaNotificacaoDto(true, true, null, identificador);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex,
                "Falha na entrega direta da notificação {Origem} pelo canal {Canal}.",
                solicitacao.Origem, solicitacao.Canal);
            return Falha(true, "Não foi possível comunicar com o provedor de WhatsApp.");
        }
    }

    private static ResultadoEntregaNotificacaoDto Falha(bool tentativaRealizada, string erro) =>
        new(tentativaRealizada, false, erro, null);

    private static string? NormalizarTelefoneBrasileiro(string telefone)
    {
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        if (digitos.Length is 10 or 11)
            digitos = $"55{digitos}";

        return digitos.Length is 12 or 13 && digitos.StartsWith("55", StringComparison.Ordinal)
            ? digitos
            : null;
    }

    private static async Task<string?> ObterIdentificadorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.TryGetProperty("key", out var key) && key.TryGetProperty("id", out var keyId))
                return keyId.GetString();
            if (root.TryGetProperty("messageId", out var messageId))
                return messageId.GetString();
            return root.TryGetProperty("id", out var id) ? id.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
