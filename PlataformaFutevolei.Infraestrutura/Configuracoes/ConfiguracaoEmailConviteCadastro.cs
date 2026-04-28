namespace PlataformaFutevolei.Infraestrutura.Configuracoes;

public class ConfiguracaoEmailConviteCadastro
{
    public const string Secao = "EmailConvitesCadastro";

    public string BaseUrl { get; set; } = "https://api.resend.com";
    public string ApiKey { get; set; } = string.Empty;
    public string RemetenteEmail { get; set; } = string.Empty;
    public string? RemetenteNome { get; set; }
    public string? ReplyTo { get; set; }
    public string UrlApp { get; set; } = string.Empty;

    public bool EstaConfigurado()
    {
        return ObterMensagemConfiguracaoIncompleta() is null;
    }

    public string? ObterMensagemConfiguracaoIncompleta()
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

        if (string.IsNullOrWhiteSpace(UrlApp))
        {
            camposAusentes.Add($"{Secao}:UrlApp");
        }

        return camposAusentes.Count == 0
            ? null
            : $"O envio automático de e-mail não está configurado. Preencha: {string.Join(", ", camposAusentes)}.";
    }

    public string ObterBaseUrl()
    {
        return string.IsNullOrWhiteSpace(BaseUrl)
            ? "https://api.resend.com"
            : BaseUrl.Trim().TrimEnd('/');
    }

    public string ObterUrlAppBase()
    {
        return UrlApp.Trim().TrimEnd('/');
    }

    public string ObterRemetenteFormatado()
    {
        var email = RemetenteEmail.Trim();
        return string.IsNullOrWhiteSpace(RemetenteNome)
            ? email
            : $"{RemetenteNome.Trim()} <{email}>";
    }
}
