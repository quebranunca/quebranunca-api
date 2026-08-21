using PlataformaFutevolei.Aplicacao.Utilitarios;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public sealed class NormalizadorTelefoneBrasileiroTests
{
    [Theory]
    [InlineData("(48) 99999-9999", "48999999999")]
    [InlineData("+55 48 99999-9999", "48999999999")]
    [InlineData("5548999999999", "48999999999")]
    public void Normalizar_FormatoValido_RetornaDddENumero(string entrada, string esperado)
        => Assert.Equal(esperado, NormalizadorTelefoneBrasileiro.Normalizar(entrada));

    [Theory]
    [InlineData("")]
    [InlineData("9999-9999")]
    [InlineData("123")]
    public void Normalizar_FormatoInvalido_RetornaNulo(string entrada)
        => Assert.Null(NormalizadorTelefoneBrasileiro.Normalizar(entrada));
}
