using PlataformaFutevolei.Dominio.Entidades;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public sealed class IdentidadeExternaUsuarioTests
{
    [Fact]
    public void NormalizaEmissorERegistraPrimeiroLogin()
    {
        var agora = new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);
        var identidade = new IdentidadeExternaUsuario(Guid.NewGuid(), "https://id.quebranunca.com/", " subject-1 ", agora);

        Assert.Equal("https://id.quebranunca.com", identidade.Emissor);
        Assert.Equal("subject-1", identidade.Subject);
        Assert.Equal(agora, identidade.VinculadaEmUtc);
        Assert.Equal(agora, identidade.UltimoLoginEmUtc);
    }

    [Fact]
    public void AtualizaDataDoUltimoLogin()
    {
        var identidade = new IdentidadeExternaUsuario(Guid.NewGuid(), "https://id.quebranunca.com", "subject-1");
        var login = new DateTime(2026, 8, 13, 13, 0, 0, DateTimeKind.Utc);

        identidade.RegistrarLogin(login);

        Assert.Equal(login, identidade.UltimoLoginEmUtc);
    }
}
