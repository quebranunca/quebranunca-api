namespace PlataformaFutevolei.Infraestrutura.Configuracoes;

public class ConfiguracaoEmailCodigoLogin
{
    public const string Secao = "EmailCodigoLogin";

    public string BaseUrl { get; set; } = "https://api.resend.com";
    public string ApiKey { get; set; } = string.Empty;
    public string RemetenteEmail { get; set; } = string.Empty;
    public string? RemetenteNome { get; set; }
    public string? ReplyTo { get; set; }

    public string? EmailOrigemSobrescrito { get; set; }
    public string? EmailDestinoSobrescrito { get; set; }

    public string? ObterMensagemConfiguracaoIncompleta(string? contexto = null)
    {
        var camposAusentes = ObterCamposConfiguracaoAusentes();

        return camposAusentes.Count == 0
            ? null
            : $"{ObterDescricaoContexto(contexto)} não está configurado. Preencha: {string.Join(", ", camposAusentes)}.";
    }

    public IReadOnlyList<string> ObterCamposConfiguracaoAusentes()
    {
        var camposAusentes = new List<string>();

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            camposAusentes.Add($"{Secao}:ApiKey");
        }

        if (string.IsNullOrWhiteSpace(RemetenteEmail))
        {
            camposAusentes.Add($"{Secao}:RemetenteEmail");
        }

        return camposAusentes;
    }

    public string ObterBaseUrl()
    {
        return string.IsNullOrWhiteSpace(BaseUrl)
            ? "https://api.resend.com"
            : BaseUrl.Trim().TrimEnd('/');
    }

    public string ObterRemetenteFormatado()
    {
        var email = RemetenteEmail.Trim();
        return string.IsNullOrWhiteSpace(RemetenteNome)
            ? email
            : $"{RemetenteNome.Trim()} <{email}>";
    }

    public string ObterEmailDestino(string emailUsuario)
    {
        if (string.IsNullOrWhiteSpace(emailUsuario))
        {
            return string.Empty;
        }

        var emailNormalizado = emailUsuario.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(EmailOrigemSobrescrito) || string.IsNullOrWhiteSpace(EmailDestinoSobrescrito))
        {
            return emailNormalizado;
        }

        var emailOrigemNormalizado = EmailOrigemSobrescrito.Trim().ToLowerInvariant();
        if (!string.Equals(emailNormalizado, emailOrigemNormalizado, StringComparison.OrdinalIgnoreCase))
        {
            return emailNormalizado;
        }

        return EmailDestinoSobrescrito.Trim().ToLowerInvariant();
    }

    public bool DeveSobrescrever(string emailUsuario)
    {
        if (string.IsNullOrWhiteSpace(emailUsuario)
            || string.IsNullOrWhiteSpace(EmailOrigemSobrescrito)
            || string.IsNullOrWhiteSpace(EmailDestinoSobrescrito))
        {
            return false;
        }

        return string.Equals(
            emailUsuario.Trim(),
            EmailOrigemSobrescrito.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ObterDescricaoContexto(string? contexto)
    {
        return string.IsNullOrWhiteSpace(contexto)
            ? "O envio automático de e-mail"
            : $"O {contexto.Trim()}";
    }
}
