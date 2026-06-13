using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Repositorios;
using Xunit;

namespace PlataformaFutevolei.Integracao.Tests;

[Collection(nameof(PostgresIntegracaoCollection))]
public class PendenciaVinculoIntegracaoTests(PostgresIntegracaoFixture fixture) : IAsyncLifetime
{
    private readonly string prefixo = $"teste-consolidacao-atleta-pendencia-{Guid.NewGuid():N}";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await fixture.LimparDadosAsync(prefixo);
    }

    [Fact]
    public async Task Pendencia_AguardandoCadastro_PersisteEmailInformado()
    {
        await using var dbContext = fixture.CriarContexto();
        var usuario = CriarUsuario("Usuario Pendencia", Email("usuario"), atleta: null);
        var atleta = CriarAtleta("Participante Informado", Email("participante"), possuiCadastroAtivo: false);
        var pendencia = new PendenciaUsuario
        {
            Usuario = usuario,
            Atleta = atleta,
            Tipo = TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
            Status = StatusPendenciaUsuario.AguardandoCadastro,
            EmailInformado = Email("informado"),
            Observacao = "Contato informado. Aguardando cadastro ativo do atleta para concluir o vínculo."
        };
        dbContext.AddRange(usuario, atleta, pendencia);
        await dbContext.SaveChangesAsync();

        await using var verificacao = fixture.CriarContexto();
        var persistida = await verificacao.PendenciasUsuarios.SingleAsync(x => x.Id == pendencia.Id);

        Assert.Equal(StatusPendenciaUsuario.AguardandoCadastro, persistida.Status);
        Assert.Equal(Email("informado"), persistida.EmailInformado);
    }

    [Fact]
    public async Task BuscarPorGrupoAsync_RetornaSomenteAtletasAtivosDoGrupo()
    {
        await using var dbContext = fixture.CriarContexto();
        var grupo = new Grupo
        {
            Nome = $"{prefixo}-grupo",
            DataInicio = DateTime.UtcNow.Date,
            Publico = false
        };

        var ativo = CriarAtletaComUsuario("Filtro Ativo", "ativo", ativo: true, dadosAnonimizados: false);
        var inativo = CriarAtletaComUsuario("Filtro Inativo", "inativo", ativo: false, dadosAnonimizados: false);
        var anonimizado = CriarAtletaComUsuario("Filtro Anonimizado", "anonimizado", ativo: true, dadosAnonimizados: true);
        var pendente = CriarAtletaComUsuario("Filtro Pendente", "pendente", ativo: true, dadosAnonimizados: false);
        pendente.CadastroPendente = true;
        var semUsuario = CriarAtleta("Filtro Sem Usuario", Email("sem-usuario"), possuiCadastroAtivo: false);
        var foraDoGrupo = CriarAtletaComUsuario("Filtro Fora", "fora", ativo: true, dadosAnonimizados: false);

        dbContext.AddRange(
            grupo,
            ativo,
            ativo.Usuario!,
            inativo,
            inativo.Usuario!,
            anonimizado,
            anonimizado.Usuario!,
            pendente,
            pendente.Usuario!,
            semUsuario,
            foraDoGrupo,
            foraDoGrupo.Usuario!,
            new GrupoAtleta { Grupo = grupo, Atleta = ativo },
            new GrupoAtleta { Grupo = grupo, Atleta = inativo },
            new GrupoAtleta { Grupo = grupo, Atleta = anonimizado },
            new GrupoAtleta { Grupo = grupo, Atleta = pendente },
            new GrupoAtleta { Grupo = grupo, Atleta = semUsuario });
        await dbContext.SaveChangesAsync();

        var resultados = await new GrupoAtletaRepositorio(dbContext)
            .BuscarPorGrupoAsync(grupo.Id, "Filtro");

        var ids = resultados.Select(x => x.AtletaId).ToList();
        Assert.Equal([ativo.Id], ids);
    }

    private string Email(string sufixo) => $"{prefixo}-{sufixo}@example.com";

    private Atleta CriarAtletaComUsuario(
        string nome,
        string sufixoEmail,
        bool ativo,
        bool dadosAnonimizados)
    {
        var atleta = CriarAtleta(nome, Email(sufixoEmail), possuiCadastroAtivo: true);
        atleta.Usuario = CriarUsuario(nome, atleta.Email!, atleta);
        atleta.Usuario.Ativo = ativo;
        atleta.Usuario.DadosAnonimizados = dadosAnonimizados;
        return atleta;
    }

    private static Atleta CriarAtleta(string nome, string email, bool possuiCadastroAtivo)
    {
        return new Atleta
        {
            Nome = nome,
            Apelido = nome.Split(' ')[1],
            Email = email,
            CadastroPendente = !possuiCadastroAtivo
        };
    }

    private static Usuario CriarUsuario(string nome, string email, Atleta? atleta)
    {
        return new Usuario
        {
            Nome = nome,
            Email = email,
            SenhaHash = "hash-teste",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            Atleta = atleta
        };
    }
}
