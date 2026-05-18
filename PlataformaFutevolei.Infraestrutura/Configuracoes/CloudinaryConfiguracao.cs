namespace PlataformaFutevolei.Infraestrutura.Configuracoes;

public class CloudinaryConfiguracao
{
    public const string Secao = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}
