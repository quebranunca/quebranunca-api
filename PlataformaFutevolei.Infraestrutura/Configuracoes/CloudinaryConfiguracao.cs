namespace PlataformaFutevolei.Infraestrutura.Configuracoes;

public class CloudinaryConfiguracao
{
    public const string Secao = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    public bool EstaConfigurado()
    {
        return !string.IsNullOrWhiteSpace(CloudName) &&
            !string.IsNullOrWhiteSpace(ApiKey) &&
            !string.IsNullOrWhiteSpace(ApiSecret);
    }
}
