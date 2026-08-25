using System.Reflection;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Api.Configuracao;
using PlataformaFutevolei.Api.Controllers;
using Xunit;

namespace PlataformaFutevolei.Api.Tests;

public class ConfiguracaoRateLimitingTests
{
    [Fact]
    public async Task Rejeicao_Retorna429RetryAfterECorrelationIdSemExporDadosSensiveis()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AdicionarRateLimitingProtecaoAbuso(new ConfigurationBuilder().Build());
        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "correlation-teste"
        };
        httpContext.Request.Path = "/api/autenticacao/login";
        httpContext.Response.Body = new MemoryStream();

        await options.OnRejected!(
            new OnRejectedContext
            {
                HttpContext = httpContext,
                Lease = new LeaseRejeitada(TimeSpan.FromSeconds(42))
            },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status429TooManyRequests, httpContext.Response.StatusCode);
        Assert.Equal("42", httpContext.Response.Headers.RetryAfter);

        httpContext.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(httpContext.Response.Body);
        var raiz = documento.RootElement;
        Assert.Equal("Muitas tentativas.", raiz.GetProperty("titulo").GetString());
        Assert.Equal("correlation-teste", raiz.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("senha", raiz.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", raiz.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(AutenticacaoController), nameof(AutenticacaoController.Login), ConfiguracaoRateLimiting.PoliticaAcesso)]
    [InlineData(typeof(AutenticacaoController), nameof(AutenticacaoController.SolicitarRedefinicaoSenha), ConfiguracaoRateLimiting.PoliticaEnvioCodigo)]
    [InlineData(typeof(ConvitesCadastroController), nameof(ConvitesCadastroController.Criar), ConfiguracaoRateLimiting.PoliticaConvites)]
    [InlineData(typeof(ConvitesCadastroController), nameof(ConvitesCadastroController.EnviarEmail), ConfiguracaoRateLimiting.PoliticaConvites)]
    [InlineData(typeof(SolicitacoesAcessoController), nameof(SolicitacoesAcessoController.Criar), ConfiguracaoRateLimiting.PoliticaCadastro)]
    public void EndpointSensivel_UsaPoliticaEsperada(Type controller, string metodo, string politica)
    {
        var methodInfo = controller.GetMethod(metodo, BindingFlags.Instance | BindingFlags.Public);
        var atributo = methodInfo?.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(atributo);
        Assert.Equal(politica, atributo.PolicyName);
    }

    private sealed class LeaseRejeitada(TimeSpan retryAfter) : RateLimitLease
    {
        private static readonly string[] NomesMetadados = [MetadataName.RetryAfter.Name];

        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => NomesMetadados;

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (metadataName == MetadataName.RetryAfter.Name)
            {
                metadata = retryAfter;
                return true;
            }

            metadata = null;
            return false;
        }
    }
}
