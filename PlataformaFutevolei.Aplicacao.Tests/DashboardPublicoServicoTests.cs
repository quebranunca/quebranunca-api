using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class DashboardPublicoServicoTests
{
    [Fact]
    public async Task ObterDashboardAsync_SemDados_RetornaResumoZerado()
    {
        var cenario = new Cenario();

        var resultado = await cenario.Servico.ObterDashboardAsync();

        Assert.Equal(0, resultado.Resumo.TotalPartidas);
        Assert.Equal(0, resultado.Resumo.TotalAtletas);
        Assert.Equal(0, resultado.Resumo.TotalGrupos);
        Assert.Equal(0, resultado.Resumo.TotalCampeonatos);
        Assert.Empty(resultado.UltimasPartidas);
        Assert.Empty(resultado.Ranking);
        Assert.Empty(resultado.AtletasDestaque);
        Assert.Empty(resultado.Grupos);
        Assert.Empty(resultado.Campeonatos);
        Assert.Empty(resultado.Regioes);
        Assert.Empty(resultado.Insights);
        Assert.Contains(resultado.Metricas, x => x.Id == "media" && x.Valor == "0,0");
    }

    [Fact]
    public async Task ObterDashboardAsync_ComPartidasValidas_MontaResumoRankingEListagens()
    {
        var agora = DateTime.UtcNow;
        var atleta1 = CriarAtleta("Ana", "Ana", "Santos", "SP");
        var atleta2 = CriarAtleta("Bia", null, "Santos", "SP");
        var atleta3 = CriarAtleta("Clara", null, "Praia Grande", "SP");
        var atleta4 = CriarAtleta("Duda", null, null, null);
        var grupo = new Grupo { Nome = "Treino manhã" };
        var arena = new Arena { Nome = "Arena Central" };
        var campeonato = new Competicao
        {
            Nome = "Open Verão",
            Tipo = TipoCompeticao.Campeonato,
            DataInicio = agora.Date.AddDays(-1),
            DataFim = agora.Date.AddDays(2),
            Arena = arena
        };
        var categoria = new CategoriaCompeticao
        {
            Nome = "Misto",
            CompeticaoId = campeonato.Id,
            Competicao = campeonato
        };
        var partidaVitoria = CriarPartida(
            atleta1,
            atleta2,
            atleta3,
            atleta4,
            grupo,
            categoria,
            agora.AddHours(-2),
            venceuDuplaA: true,
            placarA: 18,
            placarB: 16);
        var partidaDerrota = CriarPartida(
            atleta1,
            atleta2,
            atleta3,
            atleta4,
            grupo,
            categoria,
            agora.AddDays(-1),
            venceuDuplaA: false,
            placarA: 12,
            placarB: 18);
        var partidaContestada = CriarPartida(
            atleta1,
            atleta2,
            atleta3,
            atleta4,
            grupo,
            categoria,
            agora.AddMinutes(-30),
            venceuDuplaA: true,
            placarA: 18,
            placarB: 10);
        partidaContestada.StatusAprovacao = StatusAprovacaoPartida.Contestada;

        var ranking = new RankingCategoriaDto(
            categoria.Id,
            campeonato.Id,
            campeonato.Nome,
            categoria.Nome,
            GeneroCategoria.Misto,
            [
                new RankingAtletaDto(
                    1,
                    atleta1.Id,
                    atleta1.Nome,
                    atleta1.Apelido,
                    atleta1.Bairro,
                    atleta1.Cidade,
                    atleta1.Estado,
                    LadoAtleta.Esquerdo,
                    true,
                    false,
                    true,
                    "OK",
                    4,
                    3,
                    1,
                    0,
                    90,
                    0,
                    "foto.png",
                    [])
            ]);
        var cenario = new Cenario(
            [atleta1, atleta2, atleta3, atleta4],
            [campeonato],
            [grupo],
            [partidaVitoria, partidaDerrota, partidaContestada],
            [ranking]);

        var resultado = await cenario.Servico.ObterDashboardAsync();

        Assert.Equal(2, resultado.Resumo.TotalPartidas);
        Assert.Equal(4, resultado.Resumo.TotalAtletas);
        Assert.Equal(1, resultado.Resumo.TotalGrupos);
        Assert.Equal(1, resultado.Resumo.TotalCampeonatos);
        Assert.Equal(1, resultado.Resumo.PartidasHoje);
        Assert.Equal(4, resultado.Resumo.AtletasOnline);
        Assert.Equal(2, resultado.Resumo.CidadesAtivas);
        Assert.Single(resultado.Ranking);
        Assert.Equal(75, resultado.Ranking[0].Aproveitamento);
        Assert.Equal(1, resultado.Ranking[0].SequenciaAtual);
        Assert.Equal("Ana / Bia", resultado.UltimasPartidas[0].Vencedor);
        Assert.Single(resultado.Grupos);
        Assert.Equal(2, resultado.Grupos[0].Partidas);
        Assert.Single(resultado.Campeonatos);
        Assert.Equal("Em andamento", resultado.Campeonatos[0].Status);
        Assert.Contains(resultado.Regioes, x => x.Cidade == "Santos" && x.Partidas == 4);
        Assert.Contains(resultado.Metricas, x => x.Id == "disputada" && x.Valor == "18 x 16");
        Assert.Contains(resultado.Insights, x => x.Contains("lidera o ranking geral", StringComparison.OrdinalIgnoreCase));
    }

    private static Atleta CriarAtleta(string nome, string? apelido, string? cidade, string? estado)
        => new()
        {
            Nome = nome,
            Apelido = apelido,
            Cidade = cidade,
            Estado = estado
        };

    private static Partida CriarPartida(
        Atleta atleta1,
        Atleta atleta2,
        Atleta atleta3,
        Atleta atleta4,
        Grupo grupo,
        CategoriaCompeticao categoria,
        DateTime data,
        bool venceuDuplaA,
        int placarA,
        int placarB)
    {
        var duplaA = new Dupla
        {
            Atleta1Id = atleta1.Id,
            Atleta1 = atleta1,
            Atleta2Id = atleta2.Id,
            Atleta2 = atleta2
        };
        var duplaB = new Dupla
        {
            Atleta1Id = atleta3.Id,
            Atleta1 = atleta3,
            Atleta2Id = atleta4.Id,
            Atleta2 = atleta4
        };

        return new Partida
        {
            GrupoId = grupo.Id,
            Grupo = grupo,
            CategoriaCompeticaoId = categoria.Id,
            CategoriaCompeticao = categoria,
            Status = StatusPartida.Encerrada,
            StatusAprovacao = StatusAprovacaoPartida.Aprovada,
            DuplaAId = duplaA.Id,
            DuplaA = duplaA,
            DuplaBId = duplaB.Id,
            DuplaB = duplaB,
            DuplaVencedoraId = venceuDuplaA ? duplaA.Id : duplaB.Id,
            TipoRegistroResultado = TipoRegistroResultado.PlacarDetalhado,
            PlacarDuplaA = placarA,
            PlacarDuplaB = placarB,
            DataPartida = data
        };
    }

    private sealed class Cenario
    {
        public Cenario(
            IReadOnlyList<Atleta>? atletas = null,
            IReadOnlyList<Competicao>? competicoes = null,
            IReadOnlyList<Grupo>? grupos = null,
            IReadOnlyList<Partida>? partidas = null,
            IReadOnlyList<RankingCategoriaDto>? ranking = null)
        {
            Servico = new DashboardPublicoServico(
                new AtletaRepositorioStub(atletas ?? []),
                new CompeticaoRepositorioStub(competicoes ?? []),
                new GrupoRepositorioStub(grupos ?? []),
                new PartidaRepositorioStub(partidas ?? []),
                new RankingServicoStub(ranking ?? []));
        }

        public DashboardPublicoServico Servico { get; }
    }

    private sealed class AtletaRepositorioStub(IReadOnlyList<Atleta> atletas) : IAtletaRepositorio
    {
        public Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(atletas);
        public Task<int> ContarAsync(CancellationToken cancellationToken = default) => Task.FromResult(atletas.Count);
        public Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IDictionary<Guid, int>> ContarPartidasPorAtletasAsync(IEnumerable<Guid> atletaIds, CancellationToken cancellationToken = default) => Task.FromResult<IDictionary<Guid, int>>(new Dictionary<Guid, int>());
        public Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(atletas.FirstOrDefault(x => x.Id == id));
        public Task<Atleta?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(atletas.FirstOrDefault(x => x.Id == id));
        public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(atletas.FirstOrDefault(x => x.Nome == nome));
        public Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AdicionarMedidasAsync(AtletaMedidas medidas, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Atleta atleta) { }
        public void AtualizarMedidas(AtletaMedidas medidas) { }
        public void Remover(Atleta atleta) { }
    }

    private sealed class CompeticaoRepositorioStub(IReadOnlyList<Competicao> competicoes) : ICompeticaoRepositorio
    {
        public Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(competicoes);
        public Task<Competicao?> ObterGrupoResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid competicaoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(competicoes.FirstOrDefault(x => x.Nome == nome));
        public Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(competicoes.FirstOrDefault(x => x.Id == id));
        public Task<Competicao?> ObterPorIdComCategoriasAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(competicoes.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Competicao competicao) { }
        public void Remover(Competicao competicao) { }
    }

    private sealed class GrupoRepositorioStub(IReadOnlyList<Grupo> grupos) : IGrupoRepositorio
    {
        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(grupos);
        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default) => Task.FromResult(grupos);
        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(grupos.Count);
        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault(x => x.Nome == nome));
        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault(x => x.Nome == nome));
        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(grupos);
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) { }
    }

    private sealed class PartidaRepositorioStub(IReadOnlyList<Partida> partidas) : IPartidaRepositorio
    {
        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(partidas.Count);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarAdministracaoAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaParaRemocaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarReferenciandoPartidasAsync(IReadOnlyCollection<Guid> partidaIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(partidas);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(partidas.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) { }
    }

    private sealed class RankingServicoStub(IReadOnlyList<RankingCategoriaDto> ranking) : IRankingServico
    {
        public Task<RankingFiltroInicialDto> ObterFiltroInicialAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RankingFiltroInicialDto(null, null));
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasGeralAsync(CancellationToken cancellationToken = default) => Task.FromResult(ranking);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<RankingRegiaoFiltroDto> ListarRegioesDisponiveisAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RankingRegiaoFiltroDto([], [], []));
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorRegiaoAsync(string? estado, string? cidade, string? bairro, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
    }
}
