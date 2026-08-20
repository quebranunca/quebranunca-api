using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;
using PlataformaFutevolei.Infraestrutura.Servicos;
using Xunit;

namespace PlataformaFutevolei.Api.Tests;

public sealed class CentralNotificacaoWhatsappConviteCadastroServicoTests
{
    [Fact]
    public async Task EnviarAsync_CriaEProcessaNotificacaoIdempotente()
    {
        var requests = new List<(string Path, string Body, string? ApiKey)>();
        var handler = new Handler(async request =>
        {
            requests.Add((request.RequestUri!.AbsolutePath,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(),
                request.Headers.GetValues("X-Api-Key").Single()));
            return requests.Count == 1
                ? Json(HttpStatusCode.Created, """{"id":"central-1","status":"Pending"}""")
                : Json(HttpStatusCode.OK, """{"id":"central-1","status":"Sent"}""");
        });
        var service = CreateService(handler);
        var convite = CreateInvitation();

        var result = await service.EnviarAsync(convite, "123-456");

        Assert.True(result.TentativaRealizada);
        Assert.True(result.Aceito);
        Assert.True(result.Enviado);
        Assert.Equal("central-1", result.IdentificadorMensagem);
        Assert.Equal("/api/v1/notifications", requests[0].Path);
        Assert.Equal("/api/v1/notifications/central-1/process", requests[1].Path);
        Assert.Equal("client-key", requests[0].ApiKey);
        Assert.Contains("\"source\":\"quebra-nunca\"", requests[0].Body);
        Assert.Contains("\"templateKey\":\"qnf.convite.cadastro.v1\"", requests[0].Body);
        Assert.Contains("\"idempotencyKey\":\"request-1\"", requests[0].Body);
        Assert.Contains("\"codigoConvite\":\"123-456\"", requests[0].Body);
        Assert.Contains("\"linkConvite\":\"https://app.quebranunca.com.br", requests[0].Body);
    }

    [Fact]
    public async Task EnviarAsync_ProcessamentoConcorrente_RetornaAceitoSemAfirmarEnvio()
    {
        var count = 0;
        var handler = new Handler(_ => Task.FromResult(++count == 1
            ? Json(HttpStatusCode.Created, """{"id":"central-1","status":"Pending"}""")
            : Json(HttpStatusCode.Conflict, """{"detail":"already processing"}""")));
        var service = CreateService(handler);

        var result = await service.EnviarAsync(CreateInvitation(), "123-456");

        Assert.True(result.Aceito);
        Assert.False(result.Enviado);
        Assert.Null(result.Erro);
        Assert.Equal("central-1", result.IdentificadorMensagem);
    }

    private static CentralNotificacaoWhatsappConviteCadastroServico CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://hub.example/") };
        client.DefaultRequestHeaders.Add("X-Api-Key", "client-key");
        return new CentralNotificacaoWhatsappConviteCadastroServico(client, Options.Create(new ConfiguracaoWhatsappConviteCadastro
        {
            Enabled = true,
            CentralNotificacaoBaseUrl = "https://central-notificacao.example",
            CentralNotificacaoApiKey = "client-key",
            Source = "quebra-nunca",
            TemplateKey = "qnf.convite.cadastro.v1",
            UrlApp = "https://app.quebranunca.com.br"
        }), NullLogger<CentralNotificacaoWhatsappConviteCadastroServico>.Instance);
    }

    private static ConviteCadastro CreateInvitation() => new()
    {
        IdentificadorPublico = "public-id",
        Telefone = "5513999999999",
        WhatsappIdempotencyKey = "request-1"
    };

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => factory(request);
    }
}
