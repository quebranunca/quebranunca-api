namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public static class ValidadorCpf
{
    public static string? Normalizar(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return null;
        }

        var digitos = new string(cpf.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digitos) ? null : digitos;
    }

    public static bool EhValido(string? cpf)
    {
        var cpfNormalizado = Normalizar(cpf);
        if (string.IsNullOrWhiteSpace(cpfNormalizado) || cpfNormalizado.Length != 11)
        {
            return false;
        }

        if (cpfNormalizado.Distinct().Count() == 1)
        {
            return false;
        }

        var digito1 = CalcularDigitoVerificador(cpfNormalizado[..9], 10);
        var digito2 = CalcularDigitoVerificador($"{cpfNormalizado[..9]}{digito1}", 11);

        return cpfNormalizado[9] == digito1 && cpfNormalizado[10] == digito2;
    }

    private static char CalcularDigitoVerificador(string baseCpf, int pesoInicial)
    {
        var soma = 0;

        for (var indice = 0; indice < baseCpf.Length; indice++)
        {
            soma += (baseCpf[indice] - '0') * (pesoInicial - indice);
        }

        var resto = soma % 11;
        var digito = resto < 2 ? 0 : 11 - resto;
        return (char)('0' + digito);
    }
}
