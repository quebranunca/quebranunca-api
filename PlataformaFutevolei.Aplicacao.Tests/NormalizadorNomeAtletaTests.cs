using PlataformaFutevolei.Aplicacao.Utilitarios;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class NormalizadorNomeAtletaTests
{
    [Fact]
    public void NormalizarTexto_ComEspacosExtras_RemoveEspacosRedundantes()
    {
        var texto = NormalizadorNomeAtleta.NormalizarTexto("  João   da   Silva  ");

        Assert.Equal("João da Silva", texto);
    }

    [Fact]
    public void NormalizarTexto_ComAcentosEPontuacao_PreservaTextoInformado()
    {
        var texto = NormalizadorNomeAtleta.NormalizarTexto("Álvaro D'Ávila");

        Assert.Equal("Álvaro D'Ávila", texto);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizarTexto_ComNuloOuVazio_RetornaVazio(string? valor)
    {
        var texto = NormalizadorNomeAtleta.NormalizarTexto(valor);

        Assert.Equal(string.Empty, texto);
    }

    [Fact]
    public void NormalizarNomeEApelido_ComNomeEApelido_ComplementaNomeECalculaApelido()
    {
        var resultado = NormalizadorNomeAtleta.NormalizarNomeEApelido("João Silva", "Canhoto");

        Assert.Equal("João Silva Canhoto", resultado.Nome);
        Assert.Equal("João Canhoto", resultado.Apelido);
    }

    [Fact]
    public void NormalizarNomeEApelido_ComApelidoVazio_UsaNomeNormalizado()
    {
        var resultado = NormalizadorNomeAtleta.NormalizarNomeEApelido("  João   Silva  ", null);

        Assert.Equal("João Silva", resultado.Nome);
        Assert.Equal("João Silva", resultado.Apelido);
    }

    [Fact]
    public void NormalizarNomeEApelido_ComApelidoJaContidoNoNome_NaoDuplicaComplemento()
    {
        var resultado = NormalizadorNomeAtleta.NormalizarNomeEApelido("João Canhoto Silva", "Canhoto");

        Assert.Equal("João Canhoto Silva", resultado.Nome);
        Assert.Equal("João Silva", resultado.Apelido);
    }

    [Fact]
    public void NormalizarChave_ComTextoMisto_RemoveEspacosExtrasEPadronizaCaixa()
    {
        var chave = NormalizadorNomeAtleta.NormalizarChave("  João   SILVA  ");

        Assert.Equal("joão silva", chave);
    }
}
