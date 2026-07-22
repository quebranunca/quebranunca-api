using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class ProjecaoPontuacaoBeneficioCalculadorTests
{
    [Fact]
    public void Calcular_ReconstroiSaldoNegativoETotaisAPartirDoExtrato()
    {
        var extratos = new[]
        {
            CriarExtrato(23, TipoEventoPontuacaoBeneficio.PartidaParticipante),
            CriarExtrato(-20, TipoEventoPontuacaoBeneficio.ResgateBeneficio),
            CriarExtrato(-23, TipoEventoPontuacaoBeneficio.EstornoPartida)
        };

        var projecao = ProjecaoPontuacaoBeneficioCalculador.Calcular(extratos);

        Assert.Equal(-20, projecao.SaldoAtual);
        Assert.Equal(0, projecao.TotalAcumulado);
        Assert.Equal(20, projecao.TotalResgatado);
    }

    [Theory]
    [InlineData(TipoEventoPontuacaoBeneficio.PartidaParticipante, true)]
    [InlineData(TipoEventoPontuacaoBeneficio.SaldoInicialRetroativo, false)]
    [InlineData(TipoEventoPontuacaoBeneficio.EstornoPartida, false)]
    public void NormalizarParaConsolidacao_RespeitaExcecoesHistoricas(
        TipoEventoPontuacaoBeneficio tipoEvento,
        bool deveAlterar)
    {
        var perdedorId = Guid.NewGuid();
        var vencedorId = Guid.NewGuid();
        var chave = $"EVENTO:ATLETA:{perdedorId.ToString().ToUpperInvariant()}";

        var normalizada = ChaveIdempotenciaPontuacaoBeneficio.NormalizarParaConsolidacao(
            chave, tipoEvento, perdedorId, vencedorId);

        Assert.Equal(deveAlterar, normalizada.Contains(vencedorId.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.Equal(!deveAlterar, normalizada.Contains(perdedorId.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private static ExtratoPontuacaoBeneficio CriarExtrato(
        int pontos,
        TipoEventoPontuacaoBeneficio tipoEvento)
    {
        return new ExtratoPontuacaoBeneficio
        {
            AtletaId = Guid.NewGuid(),
            Pontos = pontos,
            TipoEvento = tipoEvento,
            Descricao = "Teste",
            Origem = "Teste",
            ChaveIdempotencia = Guid.NewGuid().ToString()
        };
    }
}
