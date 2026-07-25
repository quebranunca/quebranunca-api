using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Repositorios;
using Xunit;

namespace PlataformaFutevolei.Integracao.Tests;

[Collection(nameof(PostgresIntegracaoCollection))]
public class AutenticacaoCadastroIntegracaoTests(PostgresIntegracaoFixture fixture) : IAsyncLifetime
{
    private readonly string prefixo = $"teste-cadastro-publico-{Guid.NewGuid():N}";

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => fixture.LimparDadosAsync(prefixo);

    [Fact]
    public async Task CadastrosSimultaneosMesmoEmail_ApenasUmUsuarioEPersistido()
    {
        var email = $"{prefixo}@example.com";
        await using var contextoA = fixture.CriarContexto();
        await using var contextoB = fixture.CriarContexto();
        await contextoA.Usuarios.AddAsync(CriarUsuario(email));
        await contextoB.Usuarios.AddAsync(CriarUsuario(email));

        var salvamentoA = new UnidadeTrabalho(contextoA).SalvarAlteracoesAsync();
        var salvamentoB = new UnidadeTrabalho(contextoB).SalvarAlteracoesAsync();
        var resultados = await Task.WhenAll(CapturarAsync(salvamentoA), CapturarAsync(salvamentoB));

        Assert.Single(resultados.Where(x => x is null));
        var conflito = Assert.Single(resultados.Where(x => x is not null));
        Assert.IsType<RegraNegocioException>(conflito);
        Assert.Equal("Já existe um usuário cadastrado com este e-mail.", conflito!.Message);

        await using var verificacao = fixture.CriarContexto();
        Assert.Equal(1, await verificacao.Usuarios.CountAsync(x => x.Email == email));
    }

    private static Usuario CriarUsuario(string email)
        => new()
        {
            Nome = "Novo atleta",
            Email = email,
            SenhaHash = "hash-nao-reversivel-de-teste",
            SenhaDefinidaEmUtc = DateTime.UtcNow,
            SenhaAtualizadaEmUtc = DateTime.UtcNow,
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };

    private static async Task<Exception?> CapturarAsync(Task tarefa)
    {
        try
        {
            await tarefa;
            return null;
        }
        catch (Exception excecao)
        {
            return excecao;
        }
    }
}
