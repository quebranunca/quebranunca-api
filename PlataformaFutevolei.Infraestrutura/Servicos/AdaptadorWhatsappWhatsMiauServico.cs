using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public sealed class AdaptadorWhatsappWhatsMiauServico(
    HttpClient httpClient,
    IOptions<ConfiguracaoWhatsappConviteCadastro> configuracaoAccessor,
    ILogger<AdaptadorWhatsappWhatsMiauServico> logger) : IAdaptadorEntregaNotificacaoExterna
{
    private readonly ConfiguracaoWhatsappConviteCadastro configuracao = configuracaoAccessor.Value;
    public CanalNotificacaoExterna Canal => CanalNotificacaoExterna.Whatsapp;

    public async Task<ResultadoEntregaNotificacaoDto> EnviarAsync(
        SolicitacaoEntregaNotificacaoDto solicitacao, CancellationToken cancellationToken = default)
    {
        var erroConfiguracao = configuracao.ObterMensagemConfiguracaoIncompleta();
        if (erroConfiguracao is not null) return Falha(false, erroConfiguracao);
        var mensagem = MontarMensagem(solicitacao);
        if (mensagem is null)
            return Falha(false, $"O template '{solicitacao.TemplateKey}' não está registrado ou possui dados incompletos.");

        var telefone = NormalizarTelefoneBrasileiro(solicitacao.Destinatario);
        if (telefone is null) return Falha(false, "O telefone informado não é válido para envio por WhatsApp.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"message/sendText/{Uri.EscapeDataString(configuracao.ProvedorInstancia.Trim())}")
            {
                Content = JsonContent.Create(new { number = telefone, text = mensagem })
            };
            request.Headers.Add("apikey", configuracao.ProvedorApiKey.Trim());
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Falha(true, $"O provedor de WhatsApp recusou a solicitação ({(int)response.StatusCode}).");

            return new ResultadoEntregaNotificacaoDto(true, true, null,
                await ObterIdentificadorAsync(response, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Falha na entrega de WhatsApp da origem {Origem}.", solicitacao.Origem);
            return Falha(true, "Não foi possível comunicar com o provedor de WhatsApp.");
        }
    }

    private static ResultadoEntregaNotificacaoDto Falha(bool tentativa, string erro) => new(tentativa, false, erro, null);

    private static string? MontarMensagem(SolicitacaoEntregaNotificacaoDto solicitacao)
    {
        if (string.Equals(solicitacao.TemplateKey, "qnf.convite.cadastro.v1", StringComparison.Ordinal) &&
            solicitacao.Dados.TryGetValue("codigoConvite", out var codigo) &&
            solicitacao.Dados.TryGetValue("linkConvite", out var link))
        {
            return $"Você recebeu um convite para a Plataforma QuebraNunca Futevôlei.\n\nCódigo: {codigo}\nAcesse: {link}\n\nSe você não esperava esta mensagem, ignore.";
        }

        if (string.Equals(solicitacao.TemplateKey, "qnf.grupo.presenca.v1", StringComparison.Ordinal) &&
            solicitacao.Dados.TryGetValue("nomeAtleta", out var nomeAtleta) &&
            solicitacao.Dados.TryGetValue("nomeGrupo", out var nomeGrupo) &&
            solicitacao.Dados.TryGetValue("dataJogo", out var dataJogo) &&
            solicitacao.Dados.TryGetValue("horarioJogo", out var horarioJogo) &&
            solicitacao.Dados.TryGetValue("localJogo", out var localJogo) &&
            solicitacao.Dados.TryGetValue("linkConfirmacao", out var linkConfirmacao))
        {
            return $"Oi, {nomeAtleta}! O grupo {nomeGrupo} joga hoje ({dataJogo}), das {horarioJogo}, em {localJogo}.\n\nVocê vai? Confirme aqui: {linkConfirmacao}";
        }

        return null;
    }

    internal static string? NormalizarTelefoneBrasileiro(string telefone)
    {
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        if (digitos.Length is 10 or 11) digitos = $"55{digitos}";
        return digitos.Length is 12 or 13 && digitos.StartsWith("55", StringComparison.Ordinal) ? digitos : null;
    }

    private static async Task<string?> ObterIdentificadorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.TryGetProperty("key", out var key) && key.TryGetProperty("id", out var keyId)) return keyId.GetString();
            if (root.TryGetProperty("messageId", out var messageId)) return messageId.GetString();
            return root.TryGetProperty("id", out var id) ? id.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}
