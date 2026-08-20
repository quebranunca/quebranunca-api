namespace PlataformaFutevolei.Infraestrutura.Configuracoes;

public sealed class ConfiguracaoIdentityHub
{
    public const string Secao = "IdentityHub";
    public bool Habilitado { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = "quebra-nunca-web";
    public string RedirectUri { get; set; } = string.Empty;
    public string FrontendCallbackUrl { get; set; } = string.Empty;
}
