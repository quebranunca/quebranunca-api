using PlataformaFutevolei.Aplicacao.Utilitarios;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class ValidadorCpfTests
{
    [Fact]
    public void Normalizar_ComCpfFormatado_RemovePontosEHifen()
    {
        var cpf = ValidadorCpf.Normalizar("529.982.247-25");

        Assert.Equal("52998224725", cpf);
    }

    [Fact]
    public void Normalizar_ComCaracteresNaoNumericos_RemoveTudoExcetoDigitos()
    {
        var cpf = ValidadorCpf.Normalizar("CPF: 529.982.247-25!");

        Assert.Equal("52998224725", cpf);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    public void Normalizar_ComEntradaSemDigitos_RetornaNulo(string? cpf)
    {
        var normalizado = ValidadorCpf.Normalizar(cpf);

        Assert.Null(normalizado);
    }

    [Fact]
    public void EhValido_ComCpfValidoFormatado_RetornaTrue()
    {
        Assert.True(ValidadorCpf.EhValido("529.982.247-25"));
    }

    [Fact]
    public void EhValido_ComCpfValidoSemFormatacao_RetornaTrue()
    {
        Assert.True(ValidadorCpf.EhValido("52998224725"));
    }

    [Theory]
    [InlineData("529.982.247-24")]
    [InlineData("111.111.111-11")]
    [InlineData("123")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EhValido_ComCpfInvalido_RetornaFalse(string? cpf)
    {
        Assert.False(ValidadorCpf.EhValido(cpf));
    }
}
