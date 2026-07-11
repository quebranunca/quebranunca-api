using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Repositorios;
using Xunit;

namespace PlataformaFutevolei.Integracao.Tests;

[Collection(nameof(PostgresIntegracaoCollection))]
public class GrupoSelecaoIntegracaoTests(PostgresIntegracaoFixture fixture) : IAsyncLifetime
{
    private readonly string prefixo = $"teste-grupo-selecao-{Guid.NewGuid():N}";
    private readonly List<Guid> gruposSistemaCriados = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await fixture.LimparDadosAsync(prefixo);
        await using var dbContext = fixture.CriarContexto();
        var gruposSistema = await dbContext.Grupos
            .Where(x => gruposSistemaCriados.Contains(x.Id))
            .ToListAsync();
        dbContext.Grupos.RemoveRange(gruposSistema);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ListarParaSelecaoAsync_RetornaSomenteGruposPermitidosAtivosSemDuplicidade()
    {
        await using var dbContext = fixture.CriarContexto();
        var atleta = CriarAtleta("Atleta Selecionador", "selecionador");
        var usuario = CriarUsuario("Usuário Selecionador", "selecionador", atleta);
        var outroUsuario = CriarUsuario("Outro Usuário", "outro", CriarAtleta("Outro Atleta", "outro"));
        var grupoPublico = CriarGrupo("publico", publico: true);
        var grupoPrivadoMembro = CriarGrupo("privado-membro", publico: false);
        var grupoPrivadoCriador = CriarGrupo("privado-criador", publico: false, usuarioOrganizadorId: usuario.Id);
        var grupoCriadorEMembro = CriarGrupo("criador-membro", publico: false, usuarioOrganizadorId: usuario.Id);
        var grupoPrivadoTerceiro = CriarGrupo("privado-terceiro", publico: false, usuarioOrganizadorId: outroUsuario.Id);
        var grupoComHistorico = CriarGrupo("privado-historico", publico: false, usuarioOrganizadorId: outroUsuario.Id);
        var grupoInativo = CriarGrupo("publico-inativo", publico: true);
        grupoInativo.DataFim = DateTime.UtcNow.AddDays(-1);
        var grupoGeral = CriarGrupo("geral", publico: true);
        grupoGeral.Nome = "Geral";
        var grupoPartidasAvulsas = CriarGrupo("partidas-avulsas", publico: true);
        grupoPartidasAvulsas.Nome = "Partidas avulsas";
        gruposSistemaCriados.Add(grupoGeral.Id);
        gruposSistemaCriados.Add(grupoPartidasAvulsas.Id);

        var parceiro = CriarAtleta("Parceiro Histórico", "parceiro");
        var adversario1 = CriarAtleta("Adversário Um", "adversario-1");
        var adversario2 = CriarAtleta("Adversário Dois", "adversario-2");
        var duplaA = new Dupla
        {
            Nome = "Dupla Histórica A",
            Atleta1 = atleta,
            Atleta1Id = atleta.Id,
            Atleta2 = parceiro,
            Atleta2Id = parceiro.Id
        };
        var duplaB = new Dupla
        {
            Nome = "Dupla Histórica B",
            Atleta1 = adversario1,
            Atleta1Id = adversario1.Id,
            Atleta2 = adversario2,
            Atleta2Id = adversario2.Id
        };
        var partidaHistorica = new Partida
        {
            Grupo = grupoComHistorico,
            GrupoId = grupoComHistorico.Id,
            DuplaA = duplaA,
            DuplaAId = duplaA.Id,
            DuplaB = duplaB,
            DuplaBId = duplaB.Id,
            DuplaVencedora = duplaA,
            DuplaVencedoraId = duplaA.Id,
            TipoRegistroResultado = TipoRegistroResultado.ApenasResultado,
            Status = StatusPartida.Encerrada,
            StatusAprovacao = StatusAprovacaoPartida.Aprovada,
            DataPartida = DateTime.UtcNow
        };

        dbContext.AddRange(
            atleta,
            usuario,
            outroUsuario.Atleta!,
            outroUsuario,
            grupoPublico,
            grupoPrivadoMembro,
            grupoPrivadoCriador,
            grupoCriadorEMembro,
            grupoPrivadoTerceiro,
            grupoComHistorico,
            grupoInativo,
            grupoGeral,
            grupoPartidasAvulsas,
            parceiro,
            adversario1,
            adversario2,
            duplaA,
            duplaB,
            partidaHistorica,
            new GrupoAtleta { Grupo = grupoPrivadoMembro, Atleta = atleta },
            new GrupoAtleta { Grupo = grupoCriadorEMembro, Atleta = atleta });
        await dbContext.SaveChangesAsync();

        var resultados = await new GrupoRepositorio(dbContext)
            .ListarParaSelecaoAsync(usuario.Id, atleta.Id, incluirPrivadosDeTerceiros: true);

        var ids = resultados.Select(x => x.Id).ToList();
        Assert.Contains(grupoPublico.Id, ids);
        Assert.Contains(grupoPrivadoMembro.Id, ids);
        Assert.Contains(grupoPrivadoCriador.Id, ids);
        Assert.Contains(grupoCriadorEMembro.Id, ids);
        Assert.DoesNotContain(grupoPrivadoTerceiro.Id, ids);
        Assert.DoesNotContain(grupoComHistorico.Id, ids);
        Assert.DoesNotContain(grupoInativo.Id, ids);
        Assert.DoesNotContain(grupoGeral.Id, ids);
        Assert.DoesNotContain(grupoPartidasAvulsas.Id, ids);
        Assert.Single(ids.Where(id => id == grupoCriadorEMembro.Id));
    }

    private Grupo CriarGrupo(string sufixo, bool publico, Guid? usuarioOrganizadorId = null)
        => new()
        {
            Nome = $"{prefixo}-{sufixo}",
            DataInicio = DateTime.UtcNow.Date,
            Publico = publico,
            UsuarioOrganizadorId = usuarioOrganizadorId
        };

    private Atleta CriarAtleta(string nome, string sufixo)
        => new()
        {
            Nome = nome,
            Email = Email(sufixo),
            CadastroPendente = false
        };

    private Usuario CriarUsuario(string nome, string sufixo, Atleta atleta)
        => new()
        {
            Nome = nome,
            Email = Email($"usuario-{sufixo}"),
            SenhaHash = "hash",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            Atleta = atleta,
            AtletaId = atleta.Id
        };

    private string Email(string sufixo) => $"{prefixo}-{sufixo}@example.com";
}
