using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Infraestrutura.Configuracoes;
using PlataformaFutevolei.Infraestrutura.Servicos;
using Xunit;

namespace PlataformaFutevolei.Api.Tests;

public sealed class EntregaNotificacaoDiretaServicoTests
{
    [Fact]
    public async Task EnviarAsync_Whatsapp_EntregaDiretamenteAoProvedor()
    {
        HttpRequestMessage? requisicao = null;
        string? corpo = null;
        var handler = new Handler(async request =>
        {
            requisicao = request;
            corpo = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, """{"key":{"id":"mensagem-1"}}""");
        });
        var adaptador = CriarAdaptadorWhatsapp(handler);
        var servico = new EntregaNotificacaoDiretaServico([adaptador]);

        var resultado = await servico.EnviarAsync(new SolicitacaoEntregaNotificacaoDto(
            "quebra-nunca", "convite-1", CanalNotificacaoExterna.Whatsapp,
            "qnf.convite.cadastro.v1", "(13) 99999-0000",
            new Dictionary<string, string>
            {
                ["codigoConvite"] = "123-456",
                ["linkConvite"] = "https://app.quebranunca.com.br/convite/public-id"
            }));

        Assert.True(resultado.TentativaRealizada);
        Assert.True(resultado.Enviado);
        Assert.Equal("mensagem-1", resultado.IdentificadorMensagem);
        Assert.Equal("/v2/message/sendText/qnf", requisicao!.RequestUri!.AbsolutePath);
        Assert.Equal("client-key", requisicao.Headers.GetValues("apikey").Single());
        Assert.Contains("\"number\":\"5513999990000\"", corpo);
        Assert.Contains("123-456", corpo);
    }

    [Fact]
    public async Task EnviarAsync_CanalSemAdaptador_NaoTentaEntrega()
    {
        var servico = new EntregaNotificacaoDiretaServico([]);

        var resultado = await servico.EnviarAsync(new SolicitacaoEntregaNotificacaoDto(
            "quebra-nunca", "email-1", CanalNotificacaoExterna.Email,
            "template", "atleta@example.com", new Dictionary<string, string>()));

        Assert.False(resultado.TentativaRealizada);
        Assert.False(resultado.Enviado);
        Assert.Contains("não possui um adaptador", resultado.Erro);
    }

    private static AdaptadorWhatsappWhatsMiauServico CriarAdaptadorWhatsapp(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.whatsmiau.dev/v2/") },
        Options.Create(new ConfiguracaoWhatsappConviteCadastro
        {
            Enabled = true,
            Provedor = "WhatsMiau",
            ProvedorBaseUrl = "https://api.whatsmiau.dev/v2",
            ProvedorApiKey = "client-key",
            ProvedorInstancia = "qnf",
            Source = "quebra-nunca",
            TemplateKey = "qnf.convite.cadastro.v1",
            UrlApp = "https://app.quebranunca.com.br"
        }), NullLogger<AdaptadorWhatsappWhatsMiauServico>.Instance);

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
