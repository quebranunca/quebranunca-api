using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PlataformaFutevolei.Api.Middlewares;
using Xunit;

namespace PlataformaFutevolei.Api.Tests;

public class MiddlewareTratamentoErrosTests
{
    [Fact]
    public async Task InvokeAsync_ExcecaoNaoTratada_RetornaErroPadraoComCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-teste";
        context.Response.Body = new MemoryStream();

        var middleware = new MiddlewareTratamentoErros(
            _ => throw new InvalidOperationException("detalhe interno"),
            NullLogger<MiddlewareTratamentoErros>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(context.Response.Body);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal("Ocorreu um erro inesperado.", documento.RootElement.GetProperty("erro").GetString());
        Assert.Equal("trace-teste", documento.RootElement.GetProperty("correlationId").GetString());
        Assert.False(documento.RootElement.TryGetProperty("detail", out _));
    }
}
