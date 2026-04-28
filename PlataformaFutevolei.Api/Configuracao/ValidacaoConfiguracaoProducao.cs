namespace PlataformaFutevolei.Api.Configuracao;

internal static class ValidacaoConfiguracaoProducao
{
    public static void Validar(
        IHostEnvironment environment,
        string? chaveJwt,
        string? origemFrontendConfigurada,
        IReadOnlyCollection<string> origensFrontend)
    {
        if (!environment.IsProduction() && !environment.IsStaging())
        {
            return;
        }

        if (EhChaveJwtPadrao(chaveJwt))
        {
            throw new InvalidOperationException(
                "A configuração JWT de produção está inválida. Defina uma chave forte em Jwt:Chave " +
                "(ou Jwt__Chave) e não utilize o placeholder do repositório.");
        }

        if (string.IsNullOrWhiteSpace(origemFrontendConfigurada) ||
            origensFrontend.Any(ConfiguracaoCorsFrontend.EhOrigemInvalidaParaProducao))
        {
            throw new InvalidOperationException(
                "A configuração Frontend:Url é obrigatória em produção e não pode apontar para localhost. " +
                "Defina Frontend:Url (ou Frontend__Url) com a URL pública do frontend.");
        }
    }

    private static bool EhChaveJwtPadrao(string? chave)
    {
        return string.IsNullOrWhiteSpace(chave) ||
               chave.Contains("MUDAR_EM_PRODUCAO", StringComparison.OrdinalIgnoreCase);
    }
}
