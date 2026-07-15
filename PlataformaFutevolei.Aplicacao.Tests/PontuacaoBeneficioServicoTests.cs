using Microsoft.Extensions.Logging.Abstractions;
using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class PontuacaoBeneficioServicoTests
{
    [Fact]
    public async Task PontuarPartidaValidadaAsync_PartidaComPlacar_RegistraExtratoSaldoEIdempotencia()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);

        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);

        Assert.Equal(8, cenario.Repositorio.Extratos.Count);
        Assert.Equal(23, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.Equal(13, cenario.Repositorio.Saldos[cenario.OutrosAtletas[0].Id].SaldoAtual);
        Assert.Contains(cenario.Repositorio.Extratos, x => x.TipoEvento == TipoEventoPontuacaoBeneficio.PartidaPlacarCompleto);
        Assert.Contains(cenario.Repositorio.Extratos, x => x.TipoEvento == TipoEventoPontuacaoBeneficio.PartidaVitoria);
    }

    [Fact]
    public async Task PontuarPartidaValidadaAsync_PartidaPendente_NaoLiberaPontos()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        partida.StatusAprovacao = StatusAprovacaoPartida.PendenteAprovacao;

        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);

        Assert.Empty(cenario.Repositorio.Extratos);
        Assert.Empty(cenario.Repositorio.Saldos);
    }

    [Fact]
    public async Task PontuarPartidaValidadaAsync_ApenasVencedor_PontuaSemBonusDePlacar()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.ApenasResultado);

        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);

        Assert.Equal(7, cenario.Repositorio.Extratos.Count);
        Assert.DoesNotContain(cenario.Repositorio.Extratos, x => x.TipoEvento == TipoEventoPontuacaoBeneficio.PartidaPlacarCompleto);
        Assert.Equal(18, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.Contains(cenario.Repositorio.Extratos, x => x.TipoEvento == TipoEventoPontuacaoBeneficio.PartidaVitoria);
    }

    [Fact]
    public async Task SolicitarResgateAsync_SaldoInsuficiente_Bloqueia()
    {
        var cenario = new Cenario();
        var beneficio = cenario.Repositorio.AdicionarBeneficio(600);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.SolicitarResgateAsync(beneficio.Id, new SolicitarResgateBeneficioDto(null)));

        Assert.Equal("Pontos QN insuficientes para este benefício.", excecao.Message);
        Assert.Empty(cenario.Repositorio.Resgates);
    }

    [Fact]
    public async Task SolicitarResgateAsync_SaldoSuficiente_CriaResgateEExtratoNegativo()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        var beneficio = cenario.Repositorio.AdicionarBeneficio(10);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);

        var resgate = await cenario.Servico.SolicitarResgateAsync(
            beneficio.Id,
            new SolicitarResgateBeneficioDto("Quero usar na loja."));

        Assert.Equal(StatusResgateBeneficioPontuacao.Solicitado, resgate.Status);
        Assert.Null(beneficio.QuantidadeDisponivel);
        Assert.Equal(13, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.Equal(10, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].TotalResgatado);
        Assert.Contains(cenario.Repositorio.Extratos, x =>
            x.TipoEvento == TipoEventoPontuacaoBeneficio.ResgateBeneficio &&
            x.Pontos == -10 &&
            x.ResgateId == resgate.Id);
    }

    [Fact]
    public async Task SolicitarResgateAsync_ComEstoqueDisponivel_DebitaPontosEReservaEstoque()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        var beneficio = cenario.Repositorio.AdicionarBeneficio(10, quantidadeDisponivel: 1);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);

        var resgate = await cenario.Servico.SolicitarResgateAsync(
            beneficio.Id,
            new SolicitarResgateBeneficioDto(null));

        Assert.Equal(StatusResgateBeneficioPontuacao.Solicitado, resgate.Status);
        Assert.Equal(0, beneficio.QuantidadeDisponivel);
        Assert.Equal(13, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
    }

    [Fact]
    public async Task SolicitarResgateAsync_ComEstoqueZerado_BloqueiaSemDebitar()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        var beneficio = cenario.Repositorio.AdicionarBeneficio(10, quantidadeDisponivel: 0);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.SolicitarResgateAsync(beneficio.Id, new SolicitarResgateBeneficioDto(null)));

        Assert.Equal("Benefício indisponível no momento.", excecao.Message);
        Assert.Equal(0, beneficio.QuantidadeDisponivel);
        Assert.Equal(23, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.Empty(cenario.Repositorio.Resgates);
    }

    [Fact]
    public async Task SolicitarResgateAsync_DuplicadoNaoDebitaNemReservaNovamente()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        var beneficio = cenario.Repositorio.AdicionarBeneficio(10, quantidadeDisponivel: 2);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);
        await cenario.Servico.SolicitarResgateAsync(beneficio.Id, new SolicitarResgateBeneficioDto(null));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.SolicitarResgateAsync(beneficio.Id, new SolicitarResgateBeneficioDto(null)));

        Assert.Equal("Já existe um resgate solicitado para este benefício.", excecao.Message);
        Assert.Equal(1, beneficio.QuantidadeDisponivel);
        Assert.Equal(13, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.Single(cenario.Repositorio.Resgates);
        Assert.Single(cenario.Repositorio.Extratos.Where(x => x.TipoEvento == TipoEventoPontuacaoBeneficio.ResgateBeneficio));
    }

    [Fact]
    public async Task RejeitarResgateAsync_DevolvePontosEEstoqueReservado()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        var beneficio = cenario.Repositorio.AdicionarBeneficio(10, quantidadeDisponivel: 1);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);
        var resgate = await cenario.Servico.SolicitarResgateAsync(beneficio.Id, new SolicitarResgateBeneficioDto(null));
        cenario.Usuario.Perfil = PerfilUsuario.Administrador;

        var rejeitado = await cenario.Servico.RejeitarResgateAsync(
            resgate.Id,
            new AtualizarStatusResgateBeneficioDto("Sem disponibilidade operacional.", null));

        Assert.Equal(StatusResgateBeneficioPontuacao.Rejeitado, rejeitado.Status);
        Assert.Equal(1, beneficio.QuantidadeDisponivel);
        Assert.Equal(23, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.Equal(0, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].TotalResgatado);
        Assert.Single(cenario.Repositorio.Extratos.Where(x => x.TipoEvento == TipoEventoPontuacaoBeneficio.EstornoResgate));
    }

    [Fact]
    public async Task CancelarResgateAsync_DevolvePontosEEstoqueUmaVez()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        var beneficio = cenario.Repositorio.AdicionarBeneficio(10, quantidadeDisponivel: 1);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);
        var resgate = await cenario.Servico.SolicitarResgateAsync(beneficio.Id, new SolicitarResgateBeneficioDto(null));
        cenario.Usuario.Perfil = PerfilUsuario.Administrador;

        var cancelado = await cenario.Servico.CancelarResgateAsync(
            resgate.Id,
            new AtualizarStatusResgateBeneficioDto("Cancelado manualmente.", null));
        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CancelarResgateAsync(resgate.Id, new AtualizarStatusResgateBeneficioDto(null, null)));

        Assert.Equal(StatusResgateBeneficioPontuacao.Cancelado, cancelado.Status);
        Assert.Equal("Resgate não pode ser alterado neste status.", excecao.Message);
        Assert.Equal(1, beneficio.QuantidadeDisponivel);
        Assert.Equal(23, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.Equal(0, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].TotalResgatado);
        Assert.Single(cenario.Repositorio.Extratos.Where(x => x.TipoEvento == TipoEventoPontuacaoBeneficio.EstornoResgate));
    }

    [Fact]
    public async Task AprovarResgateAsync_MantemEstoqueConsumidoENaoDebitaNovamente()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        var beneficio = cenario.Repositorio.AdicionarBeneficio(10, quantidadeDisponivel: 1);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);
        var resgate = await cenario.Servico.SolicitarResgateAsync(beneficio.Id, new SolicitarResgateBeneficioDto(null));
        cenario.Usuario.Perfil = PerfilUsuario.Administrador;

        var aprovado = await cenario.Servico.AprovarResgateAsync(
            resgate.Id,
            new AtualizarStatusResgateBeneficioDto("Aprovado.", "CAMPANHA-QN"));

        Assert.Equal(StatusResgateBeneficioPontuacao.Aprovado, aprovado.Status);
        Assert.Equal(0, beneficio.QuantidadeDisponivel);
        Assert.Equal(13, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.Equal(10, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].TotalResgatado);
        Assert.DoesNotContain(cenario.Repositorio.Extratos, x => x.TipoEvento == TipoEventoPontuacaoBeneficio.EstornoResgate);
        Assert.Single(cenario.Repositorio.Extratos.Where(x => x.TipoEvento == TipoEventoPontuacaoBeneficio.ResgateBeneficio));
    }

    [Fact]
    public async Task SolicitarResgateAsync_DuasSolicitacoesParaUltimoEstoque_NaoGeraEstoqueNegativo()
    {
        var cenario = new Cenario();
        var outroUsuario = cenario.AdicionarUsuarioComAtleta("Outro usuário", "outro@qnf.test");
        var outroServico = cenario.CriarServicoParaUsuario(outroUsuario);
        var beneficio = cenario.Repositorio.AdicionarBeneficio(10, quantidadeDisponivel: 1);
        cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value] = new PontuacaoBeneficioAtleta
        {
            AtletaId = cenario.Usuario.AtletaId!.Value,
            SaldoAtual = 20,
            TotalAcumulado = 20
        };
        cenario.Repositorio.Saldos[outroUsuario.AtletaId!.Value] = new PontuacaoBeneficioAtleta
        {
            AtletaId = outroUsuario.AtletaId!.Value,
            SaldoAtual = 20,
            TotalAcumulado = 20
        };

        var resultados = await Task.WhenAll(
            SolicitarComResultadoAsync(cenario.Servico, beneficio.Id),
            SolicitarComResultadoAsync(outroServico, beneficio.Id));

        Assert.Single(resultados.Where(x => x));
        Assert.Single(resultados.Where(x => !x));
        Assert.Equal(0, beneficio.QuantidadeDisponivel);
        Assert.Single(cenario.Repositorio.Resgates);
        Assert.All(cenario.Repositorio.Saldos.Values, saldo => Assert.True(saldo.SaldoAtual >= 0));
    }

    [Fact]
    public async Task RegistrarCompartilhamentoAsync_AplicaLimiteDiarioPorTipo()
    {
        var cenario = new Cenario();

        for (var i = 0; i < 4; i++)
        {
            await cenario.Servico.RegistrarCompartilhamentoAsync(
                new RegistrarCompartilhamentoGamificacaoDto(
                    TipoCompartilhamentoGamificacao.Partida,
                    Guid.NewGuid(),
                    null,
                    null,
                    null,
                    "teste"));
        }

        Assert.Equal(3, cenario.Repositorio.Extratos.Count(x => x.TipoEvento == TipoEventoPontuacaoBeneficio.CompartilhamentoPartida));
        Assert.Equal(15, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
    }

    [Fact]
    public async Task PontuarConfirmacaoAprovacaoPartidaAsync_RegistraUmaVezPorPendencia()
    {
        var cenario = new Cenario();
        var partidaId = Guid.NewGuid();
        var pendenciaId = Guid.NewGuid();

        await cenario.Servico.PontuarConfirmacaoAprovacaoPartidaAsync(
            partidaId,
            cenario.Usuario.AtletaId!.Value,
            pendenciaId,
            cenario.Usuario.Id);
        await cenario.Servico.PontuarConfirmacaoAprovacaoPartidaAsync(
            partidaId,
            cenario.Usuario.AtletaId!.Value,
            pendenciaId,
            cenario.Usuario.Id);

        Assert.Single(cenario.Repositorio.Extratos);
        Assert.Equal(TipoEventoPontuacaoBeneficio.ConfirmacaoAprovacaoPartida, cenario.Repositorio.Extratos[0].TipoEvento);
        Assert.Equal(2, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
    }

    [Fact]
    public async Task EstornarPartidaAsync_EstornaPontosDaPartidaComIdempotencia()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);

        await cenario.Servico.EstornarPartidaAsync(partida.Id);
        await cenario.Servico.EstornarPartidaAsync(partida.Id);

        Assert.Equal(16, cenario.Repositorio.Extratos.Count);
        Assert.Equal(0, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.All(cenario.Atletas, atleta => Assert.Equal(0, cenario.Repositorio.Saldos[atleta.Id].SaldoAtual));
        Assert.Equal(8, cenario.Repositorio.Extratos.Count(x => x.TipoEvento == TipoEventoPontuacaoBeneficio.EstornoPartida));
    }

    [Fact]
    public async Task ListarMissoesEConquistas_CalculaProgressoBasico()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);

        var missoes = await cenario.Servico.ListarMissoesAsync();
        var conquistas = await cenario.Servico.ListarConquistasAsync();

        Assert.Contains(missoes, x => x.Codigo == "jogar-3-partidas" && x.ProgressoAtual == 1);
        Assert.Contains(conquistas, x => x.Codigo == "primeira-partida" && x.Desbloqueada);
    }

    [Theory]
    [InlineData(0, "Bronze", 500, 500)]
    [InlineData(499, "Bronze", 500, 1)]
    [InlineData(500, "Prata", 1500, 1000)]
    [InlineData(1499, "Prata", 1500, 1)]
    [InlineData(1500, "Ouro", 4000, 2500)]
    [InlineData(3999, "Ouro", 4000, 1)]
    [InlineData(4000, "Diamante", 8000, 4000)]
    [InlineData(7999, "Diamante", 8000, 1)]
    [InlineData(8000, "Lenda QN", null, 0)]
    public async Task ObterResumoAsync_CalculaFaixaPorTotalAcumulado(
        int totalAcumulado,
        string faixaEsperada,
        int? proximaFaixa,
        int pontosRestantes)
    {
        var cenario = new Cenario();
        cenario.DefinirSaldo(totalAcumulado, totalAcumulado);

        var resumo = await cenario.Servico.ObterResumoAsync();

        Assert.Equal(faixaEsperada, resumo.Nivel.Nome);
        Assert.Equal(proximaFaixa, resumo.Nivel.PontosProximaFaixa);
        Assert.Equal(pontosRestantes, resumo.Nivel.PontosRestantes);
        Assert.Equal(totalAcumulado, resumo.Pontuacao.TotalAcumulado);
        Assert.Contains(resumo.FaixasMedalha, faixa => faixa.Nome == "Lenda QN" && faixa.PontosMinimos == 8000);
    }

    [Fact]
    public async Task ObterResumoAsync_ResgateNaoReduzFaixaBaseadaNoTotalAcumulado()
    {
        var cenario = new Cenario();
        cenario.DefinirSaldo(saldoAtual: 100, totalAcumulado: 1500, totalResgatado: 1400);

        var resumo = await cenario.Servico.ObterResumoAsync();

        Assert.Equal(100, resumo.Pontuacao.SaldoAtual);
        Assert.Equal(1500, resumo.Pontuacao.TotalAcumulado);
        Assert.Equal(1400, resumo.Pontuacao.TotalResgatado);
        Assert.Equal("Ouro", resumo.Nivel.Nome);
        Assert.Equal(4000, resumo.Nivel.PontosProximaFaixa);
        Assert.Equal(2500, resumo.Nivel.PontosRestantes);
    }

    [Fact]
    public async Task ObterResumoAsync_ExpoeFaixasOficiaisDeMedalha()
    {
        var cenario = new Cenario();

        var resumo = await cenario.Servico.ObterResumoAsync();

        Assert.Collection(
            resumo.FaixasMedalha,
            faixa =>
            {
                Assert.Equal("Bronze", faixa.Nome);
                Assert.Equal(0, faixa.PontosMinimos);
                Assert.Equal(500, faixa.PontosProximaFaixa);
            },
            faixa =>
            {
                Assert.Equal("Prata", faixa.Nome);
                Assert.Equal(500, faixa.PontosMinimos);
                Assert.Equal(1500, faixa.PontosProximaFaixa);
            },
            faixa =>
            {
                Assert.Equal("Ouro", faixa.Nome);
                Assert.Equal(1500, faixa.PontosMinimos);
                Assert.Equal(4000, faixa.PontosProximaFaixa);
            },
            faixa =>
            {
                Assert.Equal("Diamante", faixa.Nome);
                Assert.Equal(4000, faixa.PontosMinimos);
                Assert.Equal(8000, faixa.PontosProximaFaixa);
            },
            faixa =>
            {
                Assert.Equal("Lenda QN", faixa.Nome);
                Assert.Equal(8000, faixa.PontosMinimos);
                Assert.Null(faixa.PontosProximaFaixa);
            });
    }

    [Fact]
    public void BeneficiosPadrao_MantemCampanhasPromocionaisEIncluiProdutosFisicos()
    {
        var cupom10 = Assert.Single(PontuacaoBeneficioRegras.BeneficiosPadrao, x => x.Titulo == "Cupom 10% OFF");
        var cupom20 = Assert.Single(PontuacaoBeneficioRegras.BeneficiosPadrao, x => x.Titulo == "Cupom 20% OFF");
        var chaveiro = Assert.Single(PontuacaoBeneficioRegras.BeneficiosPadrao, x => x.Titulo == "Chaveiro QuebraNunca");
        var bone = Assert.Single(PontuacaoBeneficioRegras.BeneficiosPadrao, x => x.Titulo == "Boné QuebraNunca");

        Assert.All(PontuacaoBeneficioRegras.BeneficiosPadrao, beneficio =>
        {
            Assert.False(PontuacaoBeneficioRegras.ContemCopyFinanceiraIndevida(beneficio.Titulo));
            Assert.False(PontuacaoBeneficioRegras.ContemCopyFinanceiraIndevida(beneficio.Descricao));
        });

        Assert.Equal(TipoBeneficioPontuacao.DescontoLoja, cupom10.Tipo);
        Assert.Equal(300, cupom10.PontosNecessarios);
        Assert.Null(cupom10.QuantidadeDisponivel);
        Assert.False(PontuacaoBeneficioRegras.ContemCopyFinanceiraIndevida(cupom10.Titulo));

        Assert.Equal(TipoBeneficioPontuacao.DescontoLoja, cupom20.Tipo);
        Assert.Equal(600, cupom20.PontosNecessarios);
        Assert.Equal(100, cupom20.QuantidadeDisponivel);
        Assert.False(PontuacaoBeneficioRegras.ContemCopyFinanceiraIndevida(cupom20.Titulo));

        Assert.Equal(TipoBeneficioPontuacao.Produto, chaveiro.Tipo);
        Assert.Equal(700, chaveiro.PontosNecessarios);
        Assert.Equal(100, chaveiro.QuantidadeDisponivel);
        Assert.Equal("pontos-qn/beneficio-chaveiro-qn.png", chaveiro.ImagemUrl);

        Assert.Equal(TipoBeneficioPontuacao.Produto, bone.Tipo);
        Assert.Equal(1500, bone.PontosNecessarios);
        Assert.Equal(50, bone.QuantidadeDisponivel);
        Assert.Equal("pontos-qn/beneficio-bone-qn.png", bone.ImagemUrl);
    }

    [Fact]
    public async Task ListarBeneficiosAsync_NaoExpoeCopyFinanceiraLegada()
    {
        var cenario = new Cenario();
        var beneficio = cenario.Repositorio.AdicionarBeneficio(5000);
        beneficio.Titulo = "R$ 50 off na loja";
        beneficio.Descricao = "Cupom manual de R$ 50 off para campanhas QuebraNunca.";
        beneficio.Tipo = TipoBeneficioPontuacao.DescontoLoja;

        var beneficios = await cenario.Servico.ListarBeneficiosAsync(null, null, null);
        var dto = Assert.Single(beneficios);

        Assert.Equal("Condicao especial QuebraNunca", dto.Titulo);
        Assert.Equal("Campanha promocional", dto.TipoNome);
        Assert.False(PontuacaoBeneficioRegras.ContemCopyFinanceiraIndevida(dto.Titulo));
        Assert.False(PontuacaoBeneficioRegras.ContemCopyFinanceiraIndevida(dto.Descricao));
    }

    [Fact]
    public async Task SolicitarResgateAsync_NaoExpoeTituloFinanceiroLegado()
    {
        var cenario = new Cenario();
        var partida = cenario.CriarPartidaValida(TipoRegistroResultado.PlacarDetalhado);
        var beneficio = cenario.Repositorio.AdicionarBeneficio(10);
        beneficio.Titulo = "R$ 5 off na loja";
        beneficio.Descricao = "Cupom manual de R$ 5 off para campanhas QuebraNunca.";
        beneficio.Tipo = TipoBeneficioPontuacao.DescontoLoja;
        await cenario.Servico.PontuarPartidaValidadaAsync(partida, cenario.Usuario.Id);

        var resgate = await cenario.Servico.SolicitarResgateAsync(
            beneficio.Id,
            new SolicitarResgateBeneficioDto("Quero usar em uma campanha."));

        Assert.Equal("Cupom especial QuebraNunca", resgate.BeneficioTitulo);
        Assert.False(PontuacaoBeneficioRegras.ContemCopyFinanceiraIndevida(resgate.BeneficioTitulo));
    }

    [Fact]
    public async Task RecalcularSaldoInicialRetroativoAsync_DryRunNaoAlteraSaldo()
    {
        var cenario = new Cenario(perfilUsuario: PerfilUsuario.Administrador);
        cenario.Repositorio.CalculosSaldoInicial.Add(new SaldoInicialRetroativoAtletaDto(
            cenario.Usuario.AtletaId!.Value,
            cenario.Usuario.Nome,
            2,
            1,
            1,
            1,
            1,
            1,
            true,
            113,
            false));

        var resultado = await cenario.Servico.RecalcularSaldoInicialRetroativoAsync(dryRun: true);

        Assert.True(resultado.DryRun);
        Assert.False(resultado.Aplicado);
        Assert.Equal(1, resultado.AtletasAvaliados);
        Assert.Equal(1, resultado.AtletasComSaldoInicialCalculado);
        Assert.Equal(1, resultado.AtletasComPerfilCompleto);
        Assert.Equal(113, resultado.TotalPontosCalculados);
        Assert.True(resultado.TopSaldosCalculados[0].PerfilCompleto);
        Assert.Empty(cenario.Repositorio.Extratos);
        Assert.Empty(cenario.Repositorio.Saldos);
    }

    [Fact]
    public async Task RecalcularSaldoInicialRetroativoAsync_AplicaSaldoInicialEIgnoraDuplicidade()
    {
        var cenario = new Cenario(perfilUsuario: PerfilUsuario.Administrador);
        cenario.Repositorio.CalculosSaldoInicial.Add(new SaldoInicialRetroativoAtletaDto(
            cenario.Usuario.AtletaId!.Value,
            cenario.Usuario.Nome,
            2,
            1,
            1,
            1,
            1,
            1,
            true,
            113,
            false));

        var resultado = await cenario.Servico.RecalcularSaldoInicialRetroativoAsync(dryRun: false);
        var segundaExecucao = await cenario.Servico.RecalcularSaldoInicialRetroativoAsync(dryRun: false);

        Assert.True(resultado.Aplicado);
        Assert.Equal(1, resultado.AtletasComPerfilCompleto);
        Assert.Equal(113, cenario.Repositorio.Saldos[cenario.Usuario.AtletaId!.Value].SaldoAtual);
        Assert.Single(cenario.Repositorio.Extratos);
        Assert.Equal(TipoEventoPontuacaoBeneficio.SaldoInicialRetroativo, cenario.Repositorio.Extratos[0].TipoEvento);
        Assert.Equal(1, segundaExecucao.AtletasIgnoradosPorSaldoInicialExistente);
        Assert.Equal(0, segundaExecucao.AtletasComSaldoInicialCalculado);
        Assert.Single(cenario.Repositorio.Extratos);
    }

    private static async Task<bool> SolicitarComResultadoAsync(
        PontuacaoBeneficioServico servico,
        Guid beneficioId)
    {
        try
        {
            await servico.SolicitarResgateAsync(beneficioId, new SolicitarResgateBeneficioDto(null));
            return true;
        }
        catch (RegraNegocioException ex) when (ex.Message == "Benefício indisponível no momento.")
        {
            return false;
        }
    }

    private class Cenario
    {
        public Cenario(PerfilUsuario perfilUsuario = PerfilUsuario.Atleta)
        {
            var atletaUsuario = new Atleta { Nome = "Atleta Usuário" };
            Usuario = new Usuario
            {
                Nome = "Usuário",
                Email = "usuario@qnf.test",
                Perfil = perfilUsuario,
                AtletaId = atletaUsuario.Id,
                Atleta = atletaUsuario
            };
            Usuarios.Adicionar(Usuario);
            Atletas.Add(atletaUsuario);

            OutrosAtletas =
            [
                new Atleta { Nome = "Atleta 2" },
                new Atleta { Nome = "Atleta 3" },
                new Atleta { Nome = "Atleta 4" }
            ];
            Atletas.AddRange(OutrosAtletas);
            Repositorio = new PontuacaoBeneficioRepositorioFake();
            Servico = CriarServicoParaUsuario(Usuario);
        }

        public Usuario Usuario { get; }
        public List<Atleta> Atletas { get; } = [];
        public IReadOnlyList<Atleta> OutrosAtletas { get; }
        public UsuarioRepositorioFake Usuarios { get; } = new();
        public PontuacaoBeneficioRepositorioFake Repositorio { get; }
        public UnidadeTrabalhoFake UnidadeTrabalho { get; } = new();
        public PontuacaoBeneficioServico Servico { get; }

        public Usuario AdicionarUsuarioComAtleta(string nome, string email)
        {
            var atleta = new Atleta { Nome = nome };
            var usuario = new Usuario
            {
                Nome = nome,
                Email = email,
                Perfil = PerfilUsuario.Atleta,
                AtletaId = atleta.Id,
                Atleta = atleta
            };

            Atletas.Add(atleta);
            Usuarios.Adicionar(usuario);
            return usuario;
        }

        public PontuacaoBeneficioServico CriarServicoParaUsuario(Usuario usuario)
        {
            return new PontuacaoBeneficioServico(
                Repositorio,
                Usuarios,
                UnidadeTrabalho,
                new AutorizacaoUsuarioServicoFake(usuario),
                NullLogger<PontuacaoBeneficioServico>.Instance);
        }

        public void DefinirSaldo(int saldoAtual, int totalAcumulado, int totalResgatado = 0)
        {
            Repositorio.Saldos[Usuario.AtletaId!.Value] = new PontuacaoBeneficioAtleta
            {
                AtletaId = Usuario.AtletaId!.Value,
                SaldoAtual = saldoAtual,
                TotalAcumulado = totalAcumulado,
                TotalResgatado = totalResgatado
            };
        }

        public Partida CriarPartidaValida(TipoRegistroResultado tipoRegistro)
        {
            var duplaA = new Dupla
            {
                Atleta1Id = Atletas[0].Id,
                Atleta2Id = Atletas[1].Id,
                Atleta1 = Atletas[0],
                Atleta2 = Atletas[1]
            };
            var duplaB = new Dupla
            {
                Atleta1Id = Atletas[2].Id,
                Atleta2Id = Atletas[3].Id,
                Atleta1 = Atletas[2],
                Atleta2 = Atletas[3]
            };

            return new Partida
            {
                GrupoId = Guid.NewGuid(),
                CriadoPorUsuarioId = Usuario.Id,
                DuplaAId = duplaA.Id,
                DuplaBId = duplaB.Id,
                DuplaA = duplaA,
                DuplaB = duplaB,
                DuplaVencedoraId = duplaA.Id,
                Status = StatusPartida.Encerrada,
                StatusAprovacao = StatusAprovacaoPartida.Aprovada,
                TipoRegistroResultado = tipoRegistro,
                PlacarDuplaA = tipoRegistro == TipoRegistroResultado.PlacarDetalhado ? 18 : null,
                PlacarDuplaB = tipoRegistro == TipoRegistroResultado.PlacarDetalhado ? 14 : null
            };
        }
    }

    private class PontuacaoBeneficioRepositorioFake : IPontuacaoBeneficioRepositorio
    {
        public Dictionary<Guid, PontuacaoBeneficioAtleta> Saldos { get; } = [];
        public List<ExtratoPontuacaoBeneficio> Extratos { get; } = [];
        public List<BeneficioPontuacao> Beneficios { get; } = [];
        public List<ResgateBeneficioPontuacao> Resgates { get; } = [];
        public List<SaldoInicialRetroativoAtletaDto> CalculosSaldoInicial { get; } = [];

        public BeneficioPontuacao AdicionarBeneficio(int pontos, bool ativo = true, int? quantidadeDisponivel = null)
        {
            var beneficio = new BeneficioPontuacao
            {
                Titulo = $"Benefício {pontos}",
                Descricao = "Benefício para teste.",
                Tipo = TipoBeneficioPontuacao.Brinde,
                PontosNecessarios = pontos,
                Ativo = ativo,
                QuantidadeDisponivel = quantidadeDisponivel
            };
            Beneficios.Add(beneficio);
            return beneficio;
        }

        public Task<PontuacaoBeneficioAtleta?> ObterSaldoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Saldos.GetValueOrDefault(atletaId));

        public Task<PontuacaoBeneficioAtleta?> ObterSaldoPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Saldos.GetValueOrDefault(atletaId));

        public Task AdicionarSaldoAsync(PontuacaoBeneficioAtleta saldo, CancellationToken cancellationToken = default)
        {
            Saldos[saldo.AtletaId] = saldo;
            return Task.CompletedTask;
        }

        public Task<bool> ExisteExtratoPorChaveAsync(string chaveIdempotencia, CancellationToken cancellationToken = default)
            => Task.FromResult(Extratos.Any(x => x.ChaveIdempotencia == chaveIdempotencia));

        public Task<IReadOnlyList<ExtratoPontuacaoBeneficio>> ListarExtratoAsync(
            Guid atletaId,
            TipoEventoPontuacaoBeneficio? tipo,
            DateTime? dataInicial,
            DateTime? dataFinal,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var consulta = Extratos.Where(x => x.AtletaId == atletaId);
            if (tipo.HasValue)
            {
                consulta = consulta.Where(x => x.TipoEvento == tipo.Value);
            }

            if (dataInicial.HasValue)
            {
                consulta = consulta.Where(x => x.DataCriacao >= dataInicial.Value);
            }

            if (dataFinal.HasValue)
            {
                consulta = consulta.Where(x => x.DataCriacao <= dataFinal.Value);
            }

            return Task.FromResult<IReadOnlyList<ExtratoPontuacaoBeneficio>>(
                consulta.OrderByDescending(x => x.DataCriacao).Skip(skip).Take(take).ToList());
        }

        public Task<IReadOnlyList<ExtratoPontuacaoBeneficio>> ListarExtratoPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ExtratoPontuacaoBeneficio>>(Extratos.Where(x => x.PartidaId == partidaId).ToList());

        public Task<int> ContarEventosAsync(
            Guid atletaId,
            IReadOnlyCollection<TipoEventoPontuacaoBeneficio> tipos,
            DateTime dataInicial,
            DateTime dataFinal,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Extratos.Count(x =>
                x.AtletaId == atletaId &&
                tipos.Contains(x.TipoEvento) &&
                x.DataCriacao >= dataInicial &&
                x.DataCriacao < dataFinal));

        public Task<int> SomarPontosAsync(
            Guid atletaId,
            IReadOnlyCollection<TipoEventoPontuacaoBeneficio> tipos,
            DateTime dataInicial,
            DateTime dataFinal,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Extratos.Where(x =>
                x.AtletaId == atletaId &&
                tipos.Contains(x.TipoEvento) &&
                x.DataCriacao >= dataInicial &&
                x.DataCriacao < dataFinal).Sum(x => x.Pontos));

        public Task AdicionarExtratoAsync(ExtratoPontuacaoBeneficio extrato, CancellationToken cancellationToken = default)
        {
            Extratos.Add(extrato);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SaldoInicialRetroativoAtletaDto>> CalcularSaldosIniciaisRetroativosAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SaldoInicialRetroativoAtletaDto>>(CalculosSaldoInicial);

        public Task<IReadOnlySet<Guid>> ListarAtletasComSaldoInicialRetroativoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlySet<Guid>>(Extratos
                .Where(x => x.TipoEvento == TipoEventoPontuacaoBeneficio.SaldoInicialRetroativo)
                .Select(x => x.AtletaId)
                .ToHashSet());

        public Task<IReadOnlyList<BeneficioPontuacao>> ListarBeneficiosAtivosAsync(
            TipoBeneficioPontuacao? tipo,
            bool? disponivel,
            bool? destaque,
            CancellationToken cancellationToken = default)
        {
            var consulta = Beneficios.Where(x => x.Ativo);
            if (tipo.HasValue)
            {
                consulta = consulta.Where(x => x.Tipo == tipo.Value);
            }

            if (disponivel == true)
            {
                consulta = consulta.Where(x => x.QuantidadeDisponivel is null or > 0);
            }

            if (destaque.HasValue)
            {
                consulta = consulta.Where(x => x.Destaque == destaque.Value);
            }

            return Task.FromResult<IReadOnlyList<BeneficioPontuacao>>(consulta.ToList());
        }

        public Task<BeneficioPontuacao?> ObterBeneficioPorIdAsync(Guid beneficioId, CancellationToken cancellationToken = default)
            => Task.FromResult(Beneficios.FirstOrDefault(x => x.Id == beneficioId));

        public Task<BeneficioPontuacao?> ObterBeneficioPorIdParaAtualizacaoAsync(Guid beneficioId, CancellationToken cancellationToken = default)
            => ObterBeneficioPorIdAsync(beneficioId, cancellationToken);

        public Task<IReadOnlyList<ResgateBeneficioPontuacao>> ListarResgatesPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ResgateBeneficioPontuacao>>(Resgates.Where(x => x.AtletaId == atletaId).ToList());

        public Task<IReadOnlyList<ResgateBeneficioPontuacao>> ListarResgatesAdministracaoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ResgateBeneficioPontuacao>>(Resgates);

        public Task<ResgateBeneficioPontuacao?> ObterResgatePorIdAsync(Guid resgateId, CancellationToken cancellationToken = default)
            => Task.FromResult(Resgates.FirstOrDefault(x => x.Id == resgateId));

        public Task<ResgateBeneficioPontuacao?> ObterResgatePorIdParaAtualizacaoAsync(Guid resgateId, CancellationToken cancellationToken = default)
            => ObterResgatePorIdAsync(resgateId, cancellationToken);

        public Task<bool> ExisteResgateSolicitadoAsync(Guid atletaId, Guid beneficioId, CancellationToken cancellationToken = default)
            => Task.FromResult(Resgates.Any(x =>
                x.AtletaId == atletaId &&
                x.BeneficioId == beneficioId &&
                x.Status == StatusResgateBeneficioPontuacao.Solicitado));

        public Task AdicionarResgateAsync(ResgateBeneficioPontuacao resgate, CancellationToken cancellationToken = default)
        {
            if (resgate.Beneficio is null)
            {
                var beneficio = Beneficios.FirstOrDefault(x => x.Id == resgate.BeneficioId);
                if (beneficio is not null)
                {
                    resgate.Beneficio = beneficio;
                }
            }

            Resgates.Add(resgate);
            return Task.CompletedTask;
        }

        public void AtualizarSaldo(PontuacaoBeneficioAtleta saldo)
        {
            Saldos[saldo.AtletaId] = saldo;
        }

        public void AtualizarResgate(ResgateBeneficioPontuacao resgate)
        {
        }
    }

    private class UsuarioRepositorioFake : IUsuarioRepositorio
    {
        private readonly List<Usuario> usuarios = [];

        public void Adicionar(Usuario usuario) => usuarios.Add(usuario);

        public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(usuarios);

        public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(usuarios.Count(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo));

        public Task<IReadOnlyList<Usuario>> ListarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(usuarios.Where(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo).ToList());

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(usuarios.FirstOrDefault(x => x.Email == email));

        public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default)
            => ObterPorEmailAsync(email, cancellationToken);

        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(usuarios.FirstOrDefault(x => x.Id == id));

        public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => ObterPorIdAsync(id, cancellationToken);

        public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(usuarios.FirstOrDefault(x => x.AtletaId == atletaId));

        public Task<Usuario?> ObterPorAtletaIdParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => ObterPorAtletaIdAsync(atletaId, cancellationToken);

        public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            usuarios.Add(usuario);
            return Task.CompletedTask;
        }

        public void Atualizar(Usuario usuario)
        {
        }
    }

    private class UnidadeTrabalhoFake : IUnidadeTrabalho
    {
        private readonly SemaphoreSlim semaforoTransacao = new(1, 1);

        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public async Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
        {
            await semaforoTransacao.WaitAsync(cancellationToken);
            try
            {
                await operacao(cancellationToken);
            }
            finally
            {
                semaforoTransacao.Release();
            }
        }
    }

    private class AutorizacaoUsuarioServicoFake(Usuario usuario) : IAutorizacaoUsuarioServico
    {
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(usuario);

        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(usuario);

        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default)
        {
            if (usuario.Perfil != PerfilUsuario.Administrador)
            {
                throw new AcessoNegadoException("Apenas administradores podem executar esta operação.");
            }

            return Task.CompletedTask;
        }

        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
