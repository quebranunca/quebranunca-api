using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PlataformaFutevolei.Api.Middlewares;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using Xunit;

namespace PlataformaFutevolei.Api.Tests;

public class ProtecaoAbusoDistribuidaMiddlewareTests
{
    [Fact]
    public async Task Login_ConsomeLimitesDeIpEIdentificadorSemPersistirDadosEmClaro()
    {
        var contexto = CriarContexto("/api/autenticacao/login", "{\"email\":\" Pessoa@Example.COM \",\"senha\":\"segredo\"}");
        var protecao = new ProtecaoStub(true);
        var proximoExecutado = false;
        var middleware = new ProtecaoAbusoDistribuidaMiddleware(_ =>
        {
            proximoExecutado = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(contexto, protecao, new ConfigurationBuilder().Build());

        Assert.True(proximoExecutado);
        Assert.Equal(2, protecao.Chaves.Count);
        Assert.All(protecao.Chaves, chave => Assert.DoesNotContain("Pessoa", chave, StringComparison.OrdinalIgnoreCase));
        Assert.All(protecao.Chaves, chave => Assert.DoesNotContain("203.0.113.10", chave));
        Assert.DoesNotContain(protecao.Chaves, chave => chave.Contains("segredo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_QuandoLimiteCompartilhadoEsgota_Retorna429ENaoExecutaEndpoint()
    {
        var contexto = CriarContexto("/api/autenticacao/login", "{\"email\":\"pessoa@example.com\"}");
        contexto.Response.Body = new MemoryStream();
        var protecao = new ProtecaoStub(false);
        var middleware = new ProtecaoAbusoDistribuidaMiddleware(_ => throw new InvalidOperationException("Endpoint não deveria executar."));

        await middleware.InvokeAsync(contexto, protecao, new ConfigurationBuilder().Build());

        Assert.Equal(StatusCodes.Status429TooManyRequests, contexto.Response.StatusCode);
    }

    [Fact]
    public async Task RenovacaoDeToken_NaoConsomeLimiteDeLogin()
    {
        var contexto = CriarContexto("/api/autenticacao/renovar-token", "{}");
        var protecao = new ProtecaoStub(false);
        var proximoExecutado = false;
        var middleware = new ProtecaoAbusoDistribuidaMiddleware(_ =>
        {
            proximoExecutado = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(contexto, protecao, new ConfigurationBuilder().Build());

        Assert.True(proximoExecutado);
        Assert.Empty(protecao.Chaves);
    }

    private static DefaultHttpContext CriarContexto(string caminho, string corpo)
    {
        var bytes = Encoding.UTF8.GetBytes(corpo);
        var contexto = new DefaultHttpContext();
        contexto.Request.Method = HttpMethods.Post;
        contexto.Request.Path = caminho;
        contexto.Request.ContentLength = bytes.Length;
        contexto.Request.ContentType = "application/json";
        contexto.Request.Body = new MemoryStream(bytes);
        contexto.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        return contexto;
    }

    private sealed class ProtecaoStub(bool permitir) : IProtecaoAbusoDistribuida
    {
        public List<string> Chaves { get; } = [];

        public Task<bool> TentarConsumirAsync(
            string chave,
            int limite,
            TimeSpan janela,
            CancellationToken cancellationToken = default)
        {
            Chaves.Add(chave);
            return Task.FromResult(permitir);
        }
    }
}
