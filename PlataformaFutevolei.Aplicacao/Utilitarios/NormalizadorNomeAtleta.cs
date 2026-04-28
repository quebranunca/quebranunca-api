namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public static class NormalizadorNomeAtleta
{
    public static (string Nome, string Apelido) NormalizarNomeEApelido(string nome, string? apelidoInformado)
    {
        var nomeBase = NormalizarTexto(nome);
        var complemento = NormalizarTexto(apelidoInformado);

        var nomeCompleto = nomeBase;
        if (!string.IsNullOrWhiteSpace(complemento) &&
            !nomeBase.Contains(complemento, StringComparison.OrdinalIgnoreCase))
        {
            nomeCompleto = $"{nomeBase} {complemento}";
        }

        var partesNome = nomeCompleto
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (partesNome.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        var apelido = partesNome.Length == 1
            ? partesNome[0]
            : $"{partesNome[0]} {partesNome[^1]}";

        return (nomeCompleto, apelido);
    }

    public static string NormalizarTexto(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            valor.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    public static string NormalizarChave(string? valor)
    {
        return NormalizarTexto(valor).ToLowerInvariant();
    }
}
