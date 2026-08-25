using System.Net;
using Microsoft.AspNetCore.Http;
using PlataformaFutevolei.Api.Seguranca;
using Xunit;

namespace PlataformaFutevolei.Api.Tests;

public class EnderecoIpClienteHttpTests
{
    [Fact]
    public void ProxyInternoRailway_UsaXRealIp()
    {
        var contexto = new DefaultHttpContext();
        contexto.Connection.RemoteIpAddress = IPAddress.Parse("100.64.0.5");
        contexto.Request.Headers["X-Real-IP"] = "203.0.113.42";

        Assert.Equal("203.0.113.42", EnderecoIpClienteHttp.Obter(contexto));
    }

    [Fact]
    public void OrigemNaoConfiavel_IgnoraXRealIpInformadoPeloCliente()
    {
        var contexto = new DefaultHttpContext();
        contexto.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.20");
        contexto.Request.Headers["X-Real-IP"] = "203.0.113.42";

        Assert.Equal("198.51.100.20", EnderecoIpClienteHttp.Obter(contexto));
    }
}
