namespace PlataformaFutevolei.Infraestrutura.Configuracoes;

public class ConfiguracaoJwt
{
    public const string Secao = "Jwt";

    public string Chave { get; set; } = string.Empty;
    public string Emissor { get; set; } = "PlataformaFutevolei";
    public string Audiencia { get; set; } = "PlataformaFutevolei.Web";
    public int ExpiracaoMinutos { get; set; } = 21600;
    public int ExpiracaoRefreshTokenDias { get; set; } = 90;
}
