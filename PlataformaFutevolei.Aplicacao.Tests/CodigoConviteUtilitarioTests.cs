using System.Reflection;
using System.Text.RegularExpressions;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class CodigoConviteUtilitarioTests
{
    [Fact]
    public void GerarNovo_QuandoChamado_RetornaCodigoNaoVazio()
    {
        var codigo = GerarNovo();

        Assert.False(string.IsNullOrWhiteSpace(codigo));
    }

    [Fact]
    public void GerarNovo_QuandoChamado_RetornaCodigoNoFormatoAtual()
    {
        var codigo = GerarNovo();

        Assert.Matches(new Regex(@"^\d{3}-\d{3}$"), codigo);
    }

    [Fact]
    public void Normalizar_ComEspacosHifenELetras_RetornaCodigoSemSeparadoresEMinusculo()
    {
        var codigo = Normalizar(" AbC-123 ");

        Assert.Equal("abc123", codigo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalizar_ComNuloOuVazio_RetornaVazio(string? codigoConvite)
    {
        var codigo = Normalizar(codigoConvite);

        Assert.Equal(string.Empty, codigo);
    }

    [Fact]
    public void GerarHash_ComCodigoValido_RetornaHashNaoVazio()
    {
        var hash = GerarHash("123-456");

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void GerarHash_ComMesmoCodigoNormalizado_RetornaMesmoHash()
    {
        var hashFormatado = GerarHash("123-456");
        var hashNormalizado = GerarHash(" 123456 ");

        Assert.Equal(hashFormatado, hashNormalizado);
    }

    [Fact]
    public void GerarHash_ComCodigosDiferentes_RetornaHashesDiferentes()
    {
        var hashA = GerarHash("123-456");
        var hashB = GerarHash("654-321");

        Assert.NotEqual(hashA, hashB);
    }

    private static string GerarNovo() => Invocar(nameof(GerarNovo));

    private static string Normalizar(string? codigoConvite) => Invocar(nameof(Normalizar), codigoConvite);

    private static string GerarHash(string codigoConvite) => Invocar(nameof(GerarHash), codigoConvite);

    private static string Invocar(string metodo, params object?[] parametros)
    {
        var tipo = typeof(ValidadorCpf).Assembly.GetType(
            "PlataformaFutevolei.Aplicacao.Utilitarios.CodigoConviteUtilitario",
            throwOnError: true)!;
        var metodoInfo = tipo.GetMethod(metodo, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Método {metodo} não encontrado.");

        return Assert.IsType<string>(metodoInfo.Invoke(null, parametros));
    }
}
