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

public class ResendEmailCodigoLoginServico(
    HttpClient httpClient,
    IOptions<ConfiguracaoEmailCodigoLogin> configuracaoAccessor,
    IOptions<ConfiguracaoCodigoLoginDesenvolvimento> configuracaoCodigoLoginDesenvolvimentoAccessor,
    ILogger<ResendEmailCodigoLoginServico> logger
) : IEnvioEmailCodigoLoginServico
{
    private readonly ConfiguracaoEmailCodigoLogin configuracao = configuracaoAccessor.Value;
    private readonly ConfiguracaoCodigoLoginDesenvolvimento configuracaoCodigoLoginDesenvolvimento
        = configuracaoCodigoLoginDesenvolvimentoAccessor.Value;

    public async Task<ResultadoEnvioEmailCodigoLoginDto> EnviarAsync(
        Usuario usuario,
        string codigo,
        CancellationToken cancellationToken = default)
    {
        if (EstaEmDevelopment() && configuracaoCodigoLoginDesenvolvimento.HabilitarFallbackSemEmail)
        {
            logger.LogInformation(
                "Fallback de desenvolvimento habilitado para código de login do usuário {UsuarioId}. O código não será enviado por e-mail.",
                usuario.Id);

            return new ResultadoEnvioEmailCodigoLoginDto(true, true, null, null, codigo);
        }

        var mensagemConfiguracaoIncompleta = configuracao.ObterMensagemConfiguracaoIncompleta(
            "envio do código de acesso por e-mail");
        if (mensagemConfiguracaoIncompleta is not null)
        {
            logger.LogWarning(
                "Envio do código de login para o usuário {UsuarioId} ignorado. {Mensagem}.",
                usuario.Id,
                mensagemConfiguracaoIncompleta);
            return new ResultadoEnvioEmailCodigoLoginDto(false, false, mensagemConfiguracaoIncompleta, null);
        }

        var emailDestino = configuracao.ObterEmailDestino(usuario.Email);
        if (configuracao.DeveSobrescrever(usuario.Email))
        {
            logger.LogInformation(
                "Sobrescrita de destinatário aplicada no código de login do usuário {UsuarioId}. E-mail de login: {EmailLogin}. Destino efetivo: {EmailDestino}.",
                usuario.Id,
                usuario.Email,
                emailDestino);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuracao.ApiKey.Trim());
        request.Headers.Add("Idempotency-Key", $"codigo-login-{usuario.Id:N}-{DateTime.UtcNow.Ticks}");
        request.Content = JsonContent.Create(CriarPayload(usuario, codigo, emailDestino));

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            var conteudo = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var erro = ExtrairMensagemErro(conteudo);
                logger.LogWarning(
                    "Falha ao enviar código de login do usuário {UsuarioId}. Status {StatusCode}. Erro: {Erro}.",
                    usuario.Id,
                    (int)response.StatusCode,
                    erro);
                return new ResultadoEnvioEmailCodigoLoginDto(true, false, erro, null);
            }

            var identificadorMensagem = ExtrairIdentificadorMensagem(conteudo);
            logger.LogInformation(
                "Código de login do usuário {UsuarioId} enviado com sucesso. Mensagem: {MensagemId}.",
                usuario.Id,
                identificadorMensagem);

            return new ResultadoEnvioEmailCodigoLoginDto(true, true, null, identificadorMensagem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar código de login do usuário {UsuarioId}.", usuario.Id);
            return new ResultadoEnvioEmailCodigoLoginDto(true, false, ex.Message, null);
        }
    }

    private object CriarPayload(Usuario usuario, string codigo, string emailDestino)
    {
        var assunto = ConteudoCodigoLoginEmail.MontarAssunto();
        var texto = ConteudoCodigoLoginEmail.MontarTexto(usuario, codigo);
        var html = ConteudoCodigoLoginEmail.MontarHtml(usuario, codigo);

        if (string.IsNullOrWhiteSpace(configuracao.ReplyTo))
        {
            return new
            {
                from = configuracao.ObterRemetenteFormatado(),
                to = new[] { emailDestino },
                subject = assunto,
                html,
                text = texto
            };
        }

        return new
        {
            from = configuracao.ObterRemetenteFormatado(),
            to = new[] { emailDestino },
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
            return "Falha ao enviar o código de acesso por e-mail.";
        }

        try
        {
            using var documento = JsonDocument.Parse(conteudo);
            if (documento.RootElement.TryGetProperty("message", out var mensagem))
            {
                return mensagem.GetString() ?? "Falha ao enviar o código de acesso por e-mail.";
            }

            if (documento.RootElement.TryGetProperty("error", out var erro))
            {
                return erro.GetString() ?? "Falha ao enviar o código de acesso por e-mail.";
            }
        }
        catch (JsonException)
        {
        }

        return "Falha ao enviar o código de acesso por e-mail.";
    }

    private static bool EstaEmDevelopment()
    {
        var ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        return string.Equals(ambiente, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
