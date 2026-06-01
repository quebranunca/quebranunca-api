using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class RankingServicoGrupoTests
{
    [Fact]
    public async Task ListarAtletasPorGrupoAsync_IncluiMembrosSemPartidasOuPontuacao()
    {
        var grupo = new Grupo { Nome = "Fechadinho de Quinta", DataInicio = DateTime.UtcNow };
        var atletas = new[]
        {
            new Atleta { Nome = "Atleta Com Zero", CadastroPendente = true },
            new Atleta { Nome = "Atleta Ativo" }
        };
        var gruposAtletas = new GrupoAtletaRepositorioMemoria(
            atletas.Select(atleta => new GrupoAtleta
            {
                GrupoId = grupo.Id,
                AtletaId = atleta.Id,
                Atleta = atleta
            }).ToList());
        var servico = new RankingServico(
            new LigaRepositorioStub(),
            new CompeticaoRepositorioStub(),
            new GrupoRepositorioStub(grupo),
            gruposAtletas,
            new PartidaRepositorioMemoria(),
            new AutorizacaoUsuarioServicoStub());

        var ranking = await servico.ListarAtletasPorGrupoAsync(grupo.Id);

        var categoria = Assert.Single(ranking);
        Assert.Equal("Ranking do grupo", categoria.NomeCategoria);
        Assert.Equal(2, categoria.Atletas.Count);
        Assert.All(categoria.Atletas, atleta =>
        {
            Assert.Equal(0, atleta.Pontos);
            Assert.Equal(0, atleta.Jogos);
            Assert.Equal(0, atleta.Vitorias);
            Assert.Equal(0, atleta.Derrotas);
        });
        Assert.Contains(categoria.Atletas, atleta => atleta.NomeAtleta == "Atleta Com Zero" && atleta.CadastroPendente);
    }

    private sealed class LigaRepositorioStub : ILigaRepositorio
    {
        public Task<IReadOnlyList<Liga>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Liga>>([]);
        public Task<Liga?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Liga?>(null);
        public Task<Liga?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Liga?>(null);
        public Task AdicionarAsync(Liga liga, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Liga liga) { }
        public void Remover(Liga liga) { }
    }

    private sealed class CompeticaoRepositorioStub : ICompeticaoRepositorio
    {
        public Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Competicao>>([]);
        public Task<Competicao?> ObterGrupoResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid competicaoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<Competicao?> ObterPorIdComCategoriasAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Competicao competicao) { }
        public void Remover(Competicao competicao) { }
    }

    private sealed class GrupoRepositorioStub(Grupo grupo) : IGrupoRepositorio
    {
        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(id == grupo.Id ? grupo : null);
        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) { }
    }

    private sealed class GrupoAtletaRepositorioMemoria(IReadOnlyList<GrupoAtleta> vinculos) : IGrupoAtletaRepositorio
    {
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GrupoAtleta>>(vinculos.Where(x => x.GrupoId == grupoId).ToList());

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoParaAtualizacaoAsync(Guid grupoId, CancellationToken cancellationToken = default) => ListarPorGrupoAsync(grupoId, cancellationToken);
        public Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default) => ListarPorGrupoAsync(grupoId, cancellationToken);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>(vinculos.Where(x => x.AtletaId == atletaId).ToList());
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => ListarPorAtletaAsync(atletaId, cancellationToken);
        public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(vinculos.FirstOrDefault(x => x.Id == id));
        public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(vinculos.FirstOrDefault(x => x.GrupoId == grupoId && x.AtletaId == atletaId));
        public Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remover(GrupoAtleta grupoAtleta) { }
    }

    private sealed class PartidaRepositorioMemoria : IPartidaRepositorio
    {
        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaParaRemocaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarReferenciandoPartidasAsync(IReadOnlyCollection<Guid> partidaIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => Task.FromResult(new AtletasSugestoesPartidaDto([], []));
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(new UsuarioResumoDto(string.Empty, 0, 0, 0, 0, 0, 0, 0));
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) { }
    }

    private sealed class AutorizacaoUsuarioServicoStub : IAutorizacaoUsuarioServico
    {
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(null);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
