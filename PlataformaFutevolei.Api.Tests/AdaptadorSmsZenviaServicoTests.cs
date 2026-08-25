using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Infraestrutura.Configuracoes;
using PlataformaFutevolei.Infraestrutura.Servicos;
using Xunit;

namespace PlataformaFutevolei.Api.Tests;

public sealed class AdaptadorSmsZenviaServicoTests
{
    [Fact]
    public async Task EnviarAsync_SolicitacaoValida_RetornaAceitoComIdentificador()
    {
        HttpRequestMessage? requisicao = null;
        string? corpo = null;
        var servico = CriarServico(new Handler(async request =>
        {
            requisicao = request;
            corpo = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, """{"id":"sms-1","channel":"sms"}""");
        }));

        var resultado = await servico.EnviarAsync(new SolicitacaoEntregaNotificacaoDto(
            "quebra-nunca", "codigo-1", CanalNotificacaoExterna.Sms,
            "qnf.codigo.acesso.v1", "(13) 99999-0000",
            new Dictionary<string, string> { ["texto"] = "Seu codigo QNF e 123-456." }));

        Assert.True(resultado.TentativaRealizada);
        Assert.True(resultado.Aceito);
        Assert.False(resultado.Enviado);
        Assert.Equal("sms-1", resultado.IdentificadorMensagem);
        Assert.Equal("/v1/channels/sms/messages", requisicao!.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", requisicao.Headers.Authorization!.Scheme);
        Assert.Equal("token-teste", requisicao.Headers.Authorization.Parameter);
        Assert.Contains("\"from\":\"QuebraNunca\"", corpo);
        Assert.Contains("\"to\":\"5513999990000\"", corpo);
        Assert.Contains("Seu codigo QNF e 123-456.", corpo);
    }

    [Fact]
    public async Task EnviarAsync_Desabilitado_NaoChamaZenvia()
    {
        var chamado = false;
        var servico = CriarServico(new Handler(_ =>
        {
            chamado = true;
            return Task.FromResult(Json(HttpStatusCode.OK, "{}"));
        }), enabled: false);

        var resultado = await servico.EnviarAsync(new SolicitacaoEntregaNotificacaoDto(
            "quebra-nunca", "codigo-1", CanalNotificacaoExterna.Sms,
            "qnf.codigo.acesso.v1", "13999990000",
            new Dictionary<string, string> { ["texto"] = "Codigo 123-456" }));

        Assert.False(resultado.TentativaRealizada);
        Assert.False(chamado);
    }

    private static AdaptadorSmsZenviaServico CriarServico(HttpMessageHandler handler, bool enabled = true) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.zenvia.com/v1/") },
        Options.Create(new ConfiguracaoSmsZenvia
        {
            Enabled = enabled,
            Provedor = "Zenvia",
            BaseUrl = "https://api.zenvia.com/v1",
            ApiToken = "token-teste",
            Remetente = "QuebraNunca",
            TemplateKey = "qnf.codigo.acesso.v1",
            UrlApp = "https://app.quebranunca.com.br"
        }), NullLogger<AdaptadorSmsZenviaServico>.Instance);

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
