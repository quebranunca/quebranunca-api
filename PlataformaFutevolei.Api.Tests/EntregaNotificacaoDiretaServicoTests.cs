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

    [Fact]
    public async Task EnviarAsync_PresencaGrupo_MontaMensagemComAgendaELink()
    {
        string? corpo = null;
        var handler = new Handler(async request =>
        {
            corpo = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, """{"key":{"id":"presenca-1"}}""");
        });
        var servico = new EntregaNotificacaoDiretaServico([CriarAdaptadorWhatsapp(handler)]);

        var resultado = await servico.EnviarAsync(new SolicitacaoEntregaNotificacaoDto(
            "presenca-grupo", "confirmacao-1", CanalNotificacaoExterna.Whatsapp,
            "qnf.grupo.presenca.v1", "48999999999",
            new Dictionary<string, string>
            {
                ["nomeAtleta"] = "Gus",
                ["nomeGrupo"] = "Grupo de Quarta",
                ["dataJogo"] = "02/09/2026",
                ["horarioJogo"] = "19:00 às 21:00",
                ["localJogo"] = "Arena Long Beach",
                ["linkConfirmacao"] = "https://app.quebranunca.com.br/presenca#codigo-seguro"
            }));

        Assert.True(resultado.Enviado);
        Assert.Contains("Grupo de Quarta", corpo);
        Assert.Contains("Arena Long Beach", corpo);
        Assert.Contains("19:00", corpo);
        Assert.Contains("presenca#codigo-seguro", corpo);
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
