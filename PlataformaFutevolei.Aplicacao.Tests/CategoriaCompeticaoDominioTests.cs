using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class CategoriaCompeticaoDominioTests
{
    [Fact]
    public void ObterFormatoCampeonatoEfetivo_ComFormatoProprio_RetornaFormatoDaCategoria()
    {
        var formatoCompeticao = CriarFormato("Formato competição", TipoFormatoCampeonato.PontosCorridos);
        var formatoCategoria = CriarFormato("Formato categoria", TipoFormatoCampeonato.Chave);
        var categoria = new CategoriaCompeticao
        {
            Competicao = new Competicao { Nome = "Campeonato", Tipo = TipoCompeticao.Campeonato, FormatoCampeonato = formatoCompeticao },
            FormatoCampeonato = formatoCategoria
        };

        var formato = categoria.ObterFormatoCampeonatoEfetivo();

        Assert.Same(formatoCategoria, formato);
    }

    [Fact]
    public void ObterFormatoCampeonatoEfetivo_SemFormatoProprio_RetornaFormatoDaCompeticao()
    {
        var formatoCompeticao = CriarFormato("Formato competição", TipoFormatoCampeonato.FaseDeGrupos);
        var categoria = new CategoriaCompeticao
        {
            Competicao = new Competicao { Nome = "Campeonato", Tipo = TipoCompeticao.Campeonato, FormatoCampeonato = formatoCompeticao }
        };

        var formato = categoria.ObterFormatoCampeonatoEfetivo();

        Assert.Same(formatoCompeticao, formato);
    }

    [Fact]
    public void AprovarTabelaJogos_ComUsuarioEData_PreencheControleDeAprovacao()
    {
        var categoria = new CategoriaCompeticao();
        var usuarioId = Guid.NewGuid();
        var dataAprovacao = new DateTime(2026, 6, 12, 10, 30, 0, DateTimeKind.Utc);

        categoria.AprovarTabelaJogos(usuarioId, dataAprovacao);

        Assert.True(categoria.TabelaJogosAprovada);
        Assert.Equal(usuarioId, categoria.TabelaJogosAprovadaPorUsuarioId);
        Assert.Equal(dataAprovacao, categoria.TabelaJogosAprovadaEmUtc);
    }

    [Fact]
    public void LimparAprovacaoTabelaJogos_ComAprovacaoExistente_LimpaUsuarioEData()
    {
        var categoria = new CategoriaCompeticao();
        categoria.AprovarTabelaJogos(Guid.NewGuid(), DateTime.UtcNow);

        categoria.LimparAprovacaoTabelaJogos();

        Assert.False(categoria.TabelaJogosAprovada);
        Assert.Null(categoria.TabelaJogosAprovadaPorUsuarioId);
        Assert.Null(categoria.TabelaJogosAprovadaEmUtc);
    }

    [Fact]
    public void EncerrarInscricoes_QuandoChamado_MarcaInscricoesEncerradas()
    {
        var categoria = new CategoriaCompeticao { InscricoesEncerradas = false };

        categoria.EncerrarInscricoes();

        Assert.True(categoria.InscricoesEncerradas);
    }

    [Fact]
    public void ReabrirInscricoes_QuandoChamado_MarcaInscricoesAbertas()
    {
        var categoria = new CategoriaCompeticao { InscricoesEncerradas = true };

        categoria.ReabrirInscricoes();

        Assert.False(categoria.InscricoesEncerradas);
    }

    private static FormatoCampeonato CriarFormato(string nome, TipoFormatoCampeonato tipo)
        => new()
        {
            Nome = nome,
            TipoFormato = tipo,
            Ativo = true
        };
}
