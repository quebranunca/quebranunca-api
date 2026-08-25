namespace PlataformaFutevolei.Infraestrutura.Configuracoes;

public class ConfiguracaoWhatsappConviteCadastro
{
    public const string Secao = "WhatsappConvitesCadastro";

    public bool Enabled { get; set; }
    public string Provedor { get; set; } = "WhatsMiau";
    public string ProvedorBaseUrl { get; set; } = "https://api.whatsmiau.dev/v2";
    public string ProvedorApiKey { get; set; } = string.Empty;
    public string ProvedorInstancia { get; set; } = string.Empty;
    public string Source { get; set; } = "quebra-nunca";
    public string TemplateKey { get; set; } = "qnf.convite.cadastro.v1";
    public string UrlApp { get; set; } = string.Empty;

    public string? ObterMensagemConfiguracaoIncompleta()
    {
        if (!Enabled)
        {
            return "O envio automático de WhatsApp está desabilitado. Defina WhatsappConvitesCadastro:Enabled como true para ativá-lo.";
        }

        var camposAusentes = new List<string>();

        if (!string.Equals(Provedor, "WhatsMiau", StringComparison.OrdinalIgnoreCase))
        {
            return $"O provedor de WhatsApp '{Provedor}' não é suportado.";
        }

        if (string.IsNullOrWhiteSpace(ProvedorBaseUrl))
        {
            camposAusentes.Add($"{Secao}:ProvedorBaseUrl");
        }

        if (string.IsNullOrWhiteSpace(ProvedorApiKey))
        {
            camposAusentes.Add($"{Secao}:ProvedorApiKey");
        }

        if (string.IsNullOrWhiteSpace(ProvedorInstancia))
        {
            camposAusentes.Add($"{Secao}:ProvedorInstancia");
        }

        if (string.IsNullOrWhiteSpace(Source))
        {
            camposAusentes.Add($"{Secao}:Source");
        }

        if (string.IsNullOrWhiteSpace(TemplateKey))
        {
            camposAusentes.Add($"{Secao}:TemplateKey");
        }

        if (string.IsNullOrWhiteSpace(UrlApp))
        {
            camposAusentes.Add($"{Secao}:UrlApp");
        }

        return camposAusentes.Count == 0
            ? null
            : $"O envio automático de WhatsApp não está configurado. Preencha: {string.Join(", ", camposAusentes)}.";
    }

    public string ObterUrlAppBase()
    {
        return UrlApp.Trim().TrimEnd('/');
    }
}
