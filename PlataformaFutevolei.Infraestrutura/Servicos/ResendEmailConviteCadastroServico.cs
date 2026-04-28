using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

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
        var payload = CriarPayload(conviteCadastro, linkConvite, codigoConvite);

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracao.ApiKey.Trim());
        request.Headers.Add("Idempotency-Key", $"convite-cadastro-{conviteCadastro.Id:N}-{conviteCadastro.DataAtualizacao.Ticks}");
        request.Content = JsonContent.Create(payload);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var conteudo = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var erro = ExtrairMensagemErro(conteudo);
                logger.LogWarning(
                    "Falha ao enviar e-mail automático do convite {ConviteId}. Status {StatusCode}. Erro: {Erro}.",
                    conviteCadastro.Id,
                    (int)response.StatusCode,
                    erro);
                return new ResultadoEnvioEmailConviteDto(true, false, erro, null);
            }

            var identificadorMensagem = ExtrairIdentificadorMensagem(conteudo);
            logger.LogInformation(
                "E-mail automático do convite {ConviteId} enviado com sucesso. Mensagem: {MensagemId}.",
                conviteCadastro.Id,
                identificadorMensagem);

            return new ResultadoEnvioEmailConviteDto(true, true, null, identificadorMensagem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar e-mail automático do convite {ConviteId}.", conviteCadastro.Id);
            return new ResultadoEnvioEmailConviteDto(true, false, ex.Message, null);
        }
    }

    private object CriarPayload(ConviteCadastro conviteCadastro, string linkConvite, string codigoConvite)
    {
        var assunto = ConteudoConviteCadastro.MontarAssuntoEmail();
        var texto = ConteudoConviteCadastro.MontarTextoEmail(conviteCadastro, linkConvite, codigoConvite);
        var html = ConteudoConviteCadastro.MontarHtmlEmail(conviteCadastro, linkConvite, codigoConvite);

        if (string.IsNullOrWhiteSpace(configuracao.ReplyTo))
        {
            return new
            {
                from = configuracao.ObterRemetenteFormatado(),
                to = new[] { conviteCadastro.Email },
                subject = assunto,
                html,
                text = texto
            };
        }

        return new
        {
            from = configuracao.ObterRemetenteFormatado(),
            to = new[] { conviteCadastro.Email },
            subject = assunto,
            html,
            text = texto,
            reply_to = configuracao.ReplyTo!.Trim()
        };
    }

    private static string ExtrairIdentificadorMensagem(string conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
        {
            return string.Empty;
        }

        try
        {
            using var documento = JsonDocument.Parse(conteudo);
            if (documento.RootElement.TryGetProperty("id", out var id))
            {
                return id.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }

        return string.Empty;
    }

    private static string ExtrairMensagemErro(string conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
        {
            return "Falha ao enviar o e-mail do convite.";
        }

        try
        {
            using var documento = JsonDocument.Parse(conteudo);
            if (documento.RootElement.TryGetProperty("message", out var mensagem))
            {
                return mensagem.GetString() ?? "Falha ao enviar o e-mail do convite.";
            }

            if (documento.RootElement.TryGetProperty("error", out var erro))
            {
                return erro.GetString() ?? "Falha ao enviar o e-mail do convite.";
            }
        }
        catch (JsonException)
        {
        }

        return conteudo.Length <= 500 ? conteudo : conteudo[..500];
    }
}
