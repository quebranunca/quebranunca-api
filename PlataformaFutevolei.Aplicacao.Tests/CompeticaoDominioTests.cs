using PlataformaFutevolei.Dominio.Entidades;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class CompeticaoDominioTests
{
    [Fact]
    public void ObterDiferencaMinimaPartida_ComRegraCustomizada_RetornaValorDaRegra()
    {
        var competicao = CriarCompeticaoComRegra(regra => regra.DiferencaMinimaPartida = 4);

        Assert.Equal(4, competicao.ObterDiferencaMinimaPartida());
    }

    [Fact]
    public void ObterDiferencaMinimaPartida_SemRegra_RetornaFallbackPadrao()
    {
        var competicao = new Competicao();

        Assert.Equal(Competicao.DiferencaMinimaPartidaPadrao, competicao.ObterDiferencaMinimaPartida());
    }

    [Fact]
    public void ObterPermiteEmpate_ComRegraCustomizadaTrue_RetornaTrue()
    {
        var competicao = CriarCompeticaoComRegra(regra => regra.PermiteEmpate = true);

        Assert.True(competicao.ObterPermiteEmpate());
    }

    [Fact]
    public void ObterPermiteEmpate_SemRegra_RetornaFallbackPadrao()
    {
        var competicao = new Competicao();

        Assert.Equal(Competicao.PermiteEmpatePadrao, competicao.ObterPermiteEmpate());
    }

    [Theory]
    [InlineData("primeiro", 12)]
    [InlineData("segundo", 8)]
    [InlineData("terceiro", 4)]
    public void ObterPontosColocacao_ComRegraCustomizada_RetornaValorDaRegra(string colocacao, decimal esperado)
    {
        var competicao = CriarCompeticaoComRegra(regra =>
        {
            regra.PontosPrimeiroLugar = 12m;
            regra.PontosSegundoLugar = 8m;
            regra.PontosTerceiroLugar = 4m;
        });

        var pontos = colocacao switch
        {
            "primeiro" => competicao.ObterPontosPrimeiroLugar(),
            "segundo" => competicao.ObterPontosSegundoLugar(),
            _ => competicao.ObterPontosTerceiroLugar()
        };

        Assert.Equal(esperado, pontos);
    }

    [Fact]
    public void ObterPontosPrimeiroLugar_SemRegra_RetornaFallbackPadrao()
    {
        var competicao = new Competicao();

        Assert.Equal(Competicao.PontosPrimeiroLugarPadrao, competicao.ObterPontosPrimeiroLugar());
    }

    [Fact]
    public void ObterPontosSegundoLugar_SemRegra_RetornaFallbackPadrao()
    {
        var competicao = new Competicao();

        Assert.Equal(Competicao.PontosSegundoLugarPadrao, competicao.ObterPontosSegundoLugar());
    }

    [Fact]
    public void ObterPontosTerceiroLugar_SemRegra_RetornaFallbackPadrao()
    {
        var competicao = new Competicao();

        Assert.Equal(Competicao.PontosTerceiroLugarPadrao, competicao.ObterPontosTerceiroLugar());
    }

    private static Competicao CriarCompeticaoComRegra(Action<RegraCompeticao> configurar)
    {
        var regra = new RegraCompeticao();
        configurar(regra);

        return new Competicao
        {
            RegraCompeticaoId = regra.Id,
            RegraCompeticao = regra
        };
    }
}
