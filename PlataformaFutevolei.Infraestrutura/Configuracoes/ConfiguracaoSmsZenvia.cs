namespace PlataformaFutevolei.Infraestrutura.Configuracoes;

public sealed class ConfiguracaoSmsZenvia
{
    public const string Secao = "Sms";

    public bool Enabled { get; set; }
    public string Provedor { get; set; } = "Zenvia";
    public string BaseUrl { get; set; } = "https://api.zenvia.com/v1";
    public string ApiToken { get; set; } = string.Empty;
    public string Remetente { get; set; } = string.Empty;

    public string? ObterMensagemConfiguracaoIncompleta()
    {
        if (!Enabled)
            return "O envio de SMS está desabilitado. Defina Sms:Enabled como true para ativá-lo.";
        if (!string.Equals(Provedor, "Zenvia", StringComparison.OrdinalIgnoreCase))
            return $"O provedor de SMS '{Provedor}' não é suportado.";

        var camposAusentes = new List<string>();
        if (string.IsNullOrWhiteSpace(BaseUrl)) camposAusentes.Add($"{Secao}:BaseUrl");
        if (string.IsNullOrWhiteSpace(ApiToken)) camposAusentes.Add($"{Secao}:ApiToken");
        if (string.IsNullOrWhiteSpace(Remetente)) camposAusentes.Add($"{Secao}:Remetente");

        return camposAusentes.Count == 0
            ? null
            : $"O envio de SMS não está configurado. Preencha: {string.Join(", ", camposAusentes)}.";
    }
}
