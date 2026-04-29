using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;
using Resend;

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

        try
        {
            var resend = CriarClienteResend();
            var response = await resend.EmailSendAsync(
                $"codigo-login-{usuario.Id:N}-{DateTime.UtcNow.Ticks}",
                CriarMensagem(usuario, codigo, emailDestino),
                cancellationToken);

            if (!response.Success)
            {
                var erro = ExtrairMensagemErro(response.Exception);
                logger.LogWarning(
                    "Falha ao enviar código de login do usuário {UsuarioId}. Status {StatusCode}. Erro: {Erro}.",
                    usuario.Id,
                    response.Exception?.StatusCode is null ? null : (int?)response.Exception.StatusCode,
                    erro);
                return new ResultadoEnvioEmailCodigoLoginDto(true, false, erro, null);
            }

            logger.LogInformation(
                "Código de login do usuário {UsuarioId} enviado com sucesso. Mensagem: {MensagemId}.",
                usuario.Id,
                response.Content);

            return new ResultadoEnvioEmailCodigoLoginDto(true, true, null, response.Content.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar código de login do usuário {UsuarioId}.", usuario.Id);
            return new ResultadoEnvioEmailCodigoLoginDto(true, false, ex.Message, null);
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

    private EmailMessage CriarMensagem(Usuario usuario, string codigo, string emailDestino)
    {
        var mensagem = new EmailMessage
        {
            From = configuracao.ObterRemetenteFormatado(),
            Subject = ConteudoCodigoLoginEmail.MontarAssunto(),
            TextBody = ConteudoCodigoLoginEmail.MontarTexto(usuario, codigo),
            HtmlBody = ConteudoCodigoLoginEmail.MontarHtml(usuario, codigo)
        };

        mensagem.To.Add(emailDestino);

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
            return exception.Message;
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
