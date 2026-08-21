using PlataformaFutevolei.Aplicacao.Excecoes;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public static class NormalizadorTelefoneBrasileiro
{
    public static string? Normalizar(string? telefone)
    {
        var digitos = new string((telefone ?? string.Empty).Where(char.IsDigit).ToArray());
        if ((digitos.Length is 12 or 13) && digitos.StartsWith("55", StringComparison.Ordinal))
        {
            digitos = digitos[2..];
        }

        return digitos.Length is 10 or 11 ? digitos : null;
    }

    public static string? NormalizarOpcionalOuFalhar(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
        {
            return null;
        }

        return Normalizar(telefone)
            ?? throw new RegraNegocioException("Informe um telefone brasileiro válido com DDD.");
    }

    public static string Formatar(string telefoneNormalizado)
        => telefoneNormalizado.Length == 11
            ? $"({telefoneNormalizado[..2]}) {telefoneNormalizado.Substring(2, 5)}-{telefoneNormalizado[7..]}"
            : $"({telefoneNormalizado[..2]}) {telefoneNormalizado.Substring(2, 4)}-{telefoneNormalizado[6..]}";
}
