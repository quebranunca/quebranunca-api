using System.Security.Cryptography;
using System.Text;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

internal static class CodigoConviteUtilitario
{
    public static string GerarNovo()
    {
        var codigoNormalizado = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        return string.Join(
            "-",
            Enumerable.Range(0, codigoNormalizado.Length / 4)
                .Select(indice => codigoNormalizado.Substring(indice * 4, 4)));
    }

    public static string Normalizar(string codigoConvite)
    {
        return new string((codigoConvite ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .Trim()
            .ToLowerInvariant();
    }

    public static string GerarHash(string codigoConvite)
    {
        var codigoNormalizado = Normalizar(codigoConvite);
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(codigoNormalizado));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
