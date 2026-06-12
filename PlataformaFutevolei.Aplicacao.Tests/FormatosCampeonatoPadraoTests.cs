using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class FormatosCampeonatoPadraoTests
{
    [Fact]
    public void Lista_QuandoConsultada_RetornaFormatosPadrao()
    {
        var formatos = FormatosCampeonatoPadrao.Lista;

        Assert.NotEmpty(formatos);
        Assert.Contains(formatos, x => x.TipoFormato == TipoFormatoCampeonato.PontosCorridos);
        Assert.Contains(formatos, x => x.TipoFormato == TipoFormatoCampeonato.FaseDeGrupos);
        Assert.Contains(formatos, x => x.TipoFormato == TipoFormatoCampeonato.Chave);
    }

    [Fact]
    public void Lista_FormatosPadrao_PossuemNomeTipoEStatusValidos()
    {
        var formatos = FormatosCampeonatoPadrao.Lista;

        Assert.All(formatos, formato =>
        {
            Assert.False(string.IsNullOrWhiteSpace(formato.Nome));
            Assert.True(Enum.IsDefined(formato.TipoFormato));
            Assert.True(formato.Ativo);
        });
    }

    [Theory]
    [InlineData(FormatosCampeonatoPadrao.NomePontosCorridos)]
    [InlineData(FormatosCampeonatoPadrao.NomeFaseDeGrupos)]
    [InlineData(FormatosCampeonatoPadrao.NomeChave)]
    public void EhPadrao_ComNomePadrao_RetornaTrue(string nome)
    {
        var ehPadrao = FormatosCampeonatoPadrao.EhPadrao(nome);

        Assert.True(ehPadrao);
    }

    [Fact]
    public void EhPadrao_ComNomeDesconhecido_RetornaFalse()
    {
        var ehPadrao = FormatosCampeonatoPadrao.EhPadrao("Formato personalizado");

        Assert.False(ehPadrao);
    }

    [Theory]
    [InlineData(TipoFormatoCampeonato.PontosCorridos, FormatosCampeonatoPadrao.NomePontosCorridos)]
    [InlineData(TipoFormatoCampeonato.FaseDeGrupos, FormatosCampeonatoPadrao.NomeFaseDeGrupos)]
    [InlineData(TipoFormatoCampeonato.Chave, FormatosCampeonatoPadrao.NomeChave)]
    public void ObterNomePadraoPorTipo_ComTipoSuportado_RetornaNomeEsperado(
        TipoFormatoCampeonato tipoFormato,
        string nomeEsperado)
    {
        var nome = FormatosCampeonatoPadrao.ObterNomePadraoPorTipo(tipoFormato);

        Assert.Equal(nomeEsperado, nome);
    }

    [Fact]
    public void ObterNomePadraoPorTipo_ComTipoInvalido_RetornaNulo()
    {
        var nome = FormatosCampeonatoPadrao.ObterNomePadraoPorTipo((TipoFormatoCampeonato)999);

        Assert.Null(nome);
    }
}
