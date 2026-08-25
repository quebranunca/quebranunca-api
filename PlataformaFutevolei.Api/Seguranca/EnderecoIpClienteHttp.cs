using System.Net;

namespace PlataformaFutevolei.Api.Seguranca;

public static class EnderecoIpClienteHttp
{
    private const string CabecalhoIpRealRailway = "X-Real-IP";

    public static string? Obter(HttpContext context)
    {
        var remoto = context.Connection.RemoteIpAddress;
        if (EhProxyInternoRailway(remoto)
            && IPAddress.TryParse(context.Request.Headers[CabecalhoIpRealRailway].FirstOrDefault(), out var ipReal))
        {
            return ipReal.ToString();
        }

        return remoto?.ToString();
    }

    private static bool EhProxyInternoRailway(IPAddress? endereco)
    {
        if (endereco is null) return false;
        if (endereco.IsIPv4MappedToIPv6) endereco = endereco.MapToIPv4();
        return endereco.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            && endereco.GetAddressBytes()[0] == 100;
    }
}
