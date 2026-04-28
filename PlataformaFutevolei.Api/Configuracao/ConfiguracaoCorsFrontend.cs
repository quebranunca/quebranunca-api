namespace PlataformaFutevolei.Api.Configuracao;

internal static class ConfiguracaoCorsFrontend
{
    private static readonly string[] OrigensPadraoDesenvolvimento =
    {
        "http://localhost:5173"
    };

    public static string[] ObterOrigens(string? origemFrontendConfigurada)
    {
        var origensConfiguradas = string.IsNullOrWhiteSpace(origemFrontendConfigurada)
            ? OrigensPadraoDesenvolvimento
            : origemFrontendConfigurada
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return origensConfiguradas
            .Select(NormalizarOrigem)
            .Where(origem => !string.IsNullOrWhiteSpace(origem))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool EhOrigemPermitida(string? origem, IReadOnlyCollection<string> origensPermitidas)
    {
        if (string.IsNullOrWhiteSpace(origem))
        {
            return false;
        }

        var origemNormalizada = NormalizarOrigem(origem);
        if (string.IsNullOrWhiteSpace(origemNormalizada))
        {
            return false;
        }

        return origensPermitidas.Contains(origemNormalizada, StringComparer.OrdinalIgnoreCase);
    }

    public static bool EhOrigemInvalidaParaProducao(string origem)
    {
        if (!Uri.TryCreate(origem, UriKind.Absolute, out var uri))
        {
            return true;
        }

        return uri.IsLoopback;
    }

    private static string NormalizarOrigem(string origem)
    {
        if (!Uri.TryCreate(origem, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Scheme) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return string.Empty;
        }

        var portaPadrao = uri.IsDefaultPort
            ? string.Empty
            : $":{uri.Port}";

        return $"{uri.Scheme}://{uri.Host}{portaPadrao}";
    }
}
