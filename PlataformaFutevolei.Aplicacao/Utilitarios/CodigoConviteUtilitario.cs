using System.Security.Cryptography;
using System.Text;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

internal static class CodigoConviteUtilitario
{
    public static string GerarNovo()
    {
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        return $"{codigo[..3]}-{codigo[3..]}";
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
