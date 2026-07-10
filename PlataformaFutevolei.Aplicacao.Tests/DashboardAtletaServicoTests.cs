using Microsoft.Extensions.Logging.Abstractions;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class DashboardAtletaServicoTests
{
    [Fact]
    public async Task ObterResumoAsync_UsuarioSemAtleta_RetornaErroControlado()
    {
        var servico = CriarServico(usuario: new Usuario { Nome = "Organizador", AtletaId = null });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => servico.ObterResumoAsync());

        Assert.Equal("Seu usuário precisa estar vinculado a um atleta para visualizar o dashboard.", excecao.Message);
    }

    [Fact]
    public async Task ObterResumoAsync_AtletaSemPartidas_RetornaResumoZerado()
    {
        var atleta = new Atleta { Nome = "Atleta Sem Partidas" };
        var servico = CriarServico(atleta: atleta);

        var resumo = await servico.ObterResumoAsync();

        AssertResumoZerado(resumo);
    }

    [Fact]
    public async Task ObterResumoAsync_AtletaComPartida_RetornaMetricasDoHistorico()
    {
        var atleta = new Atleta { Nome = "Ana Silva", Apelido = "Ana" };
        var parceiro = new Atleta { Nome = "Bruno Costa", Apelido = "Bruno" };
        var rival1 = new Atleta { Nome = "Carlos Lima", Apelido = "Carlos" };
        var rival2 = new Atleta { Nome = "Daniel Rocha", Apelido = "Daniel" };
        var partida = CriarPartidaEncerrada(atleta, parceiro, rival1, rival2, atletaVenceu: true, placarA: 18, placarB: 12);
        var servico = CriarServico(atleta: atleta, partidas: [partida]);

        var resumo = await servico.ObterResumoAsync();

        Assert.Equal(1, resumo.TotalPartidas);
        Assert.Equal(1, resumo.Vitorias);
        Assert.Equal(0, resumo.Derrotas);
        Assert.Equal(100, resumo.Aproveitamento);
        Assert.Equal(6, resumo.SaldoPontos);
        Assert.Equal(1, resumo.SequenciaAtual);
        Assert.Equal("Bruno", resumo.MelhorParceiro);
        Assert.Equal("Carlos", resumo.RivalMaisFrequente);
    }

    [Fact]
    public async Task ObterResumoAsync_PartidaCancelada_IgnoraNoHistoricoEsportivo()
    {
        var atleta = new Atleta { Nome = "Ana Silva", Apelido = "Ana" };
        var parceiro = new Atleta { Nome = "Bruno Costa", Apelido = "Bruno" };
        var rival1 = new Atleta { Nome = "Carlos Lima", Apelido = "Carlos" };
        var rival2 = new Atleta { Nome = "Daniel Rocha", Apelido = "Daniel" };
        var partida = CriarPartidaEncerrada(atleta, parceiro, rival1, rival2, atletaVenceu: true, placarA: 18, placarB: 12);
        partida.Cancelada = true;
        partida.CanceladaEm = DateTime.UtcNow;
        var servico = CriarServico(atleta: atleta, partidas: [partida]);

        var resumo = await servico.ObterResumoAsync();

        AssertResumoZerado(resumo);
    }

    [Fact]
    public async Task ObterDashboardAsync_AtletaComPartidaSemRanking_NaoGeraErro()
    {
        var atleta = new Atleta { Nome = "Ana Silva" };
        var partida = CriarPartidaEncerrada(
            atleta,
            new Atleta { Nome = "Bruno Costa" },
            new Atleta { Nome = "Carlos Lima" },
            new Atleta { Nome = "Daniel Rocha" },
            atletaVenceu: true,
            placarA: 18,
            placarB: 12);
        var servico = CriarServico(atleta: atleta, partidas: [partida], ranking: []);

        var dashboard = await servico.ObterDashboardAsync();

        Assert.Null(dashboard.Perfil.PosicaoRanking);
        Assert.Equal(1, dashboard.Resumo.TotalPartidas);
    }

    [Fact]
    public async Task ObterResumoAsync_DadosIncompletosNaPartida_IgnoraPartidaSemGerarErro500()
    {
        var atleta = new Atleta { Nome = "Ana Silva" };
        var partidaIncompleta = new Partida
        {
            Status = StatusPartida.Encerrada,
            StatusAprovacao = StatusAprovacaoPartida.Aprovada,
            DuplaA = new Dupla(),
            DuplaB = new Dupla(),
            DuplaVencedoraId = Guid.NewGuid(),
            TipoRegistroResultado = TipoRegistroResultado.PlacarDetalhado,
            PlacarDuplaA = 18,
            PlacarDuplaB = 16
        };
        var servico = CriarServico(atleta: atleta, partidas: [partidaIncompleta]);

        var resumo = await servico.ObterResumoAsync();

        AssertResumoZerado(resumo);
    }

    private static DashboardAtletaServico CriarServico(
        Usuario? usuario = null,
        Atleta? atleta = null,
        IReadOnlyList<Partida>? partidas = null,
        IReadOnlyList<RankingCategoriaDto>? ranking = null)
    {
        usuario ??= new Usuario { Nome = "Usuário Atleta", AtletaId = atleta?.Id };
        if (atleta is not null)
        {
            usuario.AtletaId = atleta.Id;
        }

        return new DashboardAtletaServico(
            new AtletaRepositorioStub(atleta),
            new PartidaRepositorioStub(partidas ?? []),
            new RankingServicoStub(ranking ?? []),
            new AutorizacaoUsuarioServicoStub(usuario),
            NullLogger<DashboardAtletaServico>.Instance);
    }

    private static Partida CriarPartidaEncerrada(
        Atleta atleta,
        Atleta parceiro,
        Atleta rival1,
        Atleta rival2,
        bool atletaVenceu,
        int? placarA = null,
        int? placarB = null)
    {
        var duplaAtleta = new Dupla
        {
            Atleta1Id = atleta.Id,
            Atleta1 = atleta,
            Atleta2Id = parceiro.Id,
            Atleta2 = parceiro
        };
        var duplaRival = new Dupla
        {
            Atleta1Id = rival1.Id,
            Atleta1 = rival1,
            Atleta2Id = rival2.Id,
            Atleta2 = rival2
        };

        return new Partida
        {
            Status = StatusPartida.Encerrada,
            StatusAprovacao = StatusAprovacaoPartida.Aprovada,
            DuplaAId = duplaAtleta.Id,
            DuplaA = duplaAtleta,
            DuplaBId = duplaRival.Id,
            DuplaB = duplaRival,
            DuplaVencedoraId = atletaVenceu ? duplaAtleta.Id : duplaRival.Id,
            TipoRegistroResultado = placarA.HasValue && placarB.HasValue
                ? TipoRegistroResultado.PlacarDetalhado
                : TipoRegistroResultado.ApenasResultado,
            PlacarDuplaA = placarA,
            PlacarDuplaB = placarB,
            DataPartida = DateTime.UtcNow
        };
    }

    private static void AssertResumoZerado(DashboardAtletaResumoDto resumo)
    {
        Assert.Equal(0, resumo.TotalPartidas);
        Assert.Equal(0, resumo.Vitorias);
        Assert.Equal(0, resumo.Derrotas);
        Assert.Equal(0, resumo.Aproveitamento);
        Assert.Equal(0, resumo.SaldoPontos);
        Assert.Equal(0, resumo.SequenciaAtual);
        Assert.Null(resumo.MelhorParceiro);
        Assert.Null(resumo.RivalMaisFrequente);
    }

    private sealed class AtletaRepositorioStub(Atleta? atleta) : IAtletaRepositorio
    {
        public Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<int> ContarAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IDictionary<Guid, int>> ContarPartidasPorAtletasAsync(IEnumerable<Guid> atletaIds, CancellationToken cancellationToken = default) => Task.FromResult<IDictionary<Guid, int>>(new Dictionary<Guid, int>());
        public Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(atleta is not null && atleta.Id == id ? atleta : null);
        public Task<Atleta?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default) => ObterPorIdAsync(id, cancellationToken);
        public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Atleta?>(null);
        public Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AdicionarMedidasAsync(AtletaMedidas medidas, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Atleta atleta) { }
        public void AtualizarMedidas(AtletaMedidas medidas) { }
        public void Remover(Atleta atleta) { }
    }

    private sealed class PartidaRepositorioStub(IReadOnlyList<Partida> partidas) : IPartidaRepositorio
    {
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(partidas);
        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
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
        public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
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

    private sealed class AutorizacaoUsuarioServicoStub(Usuario? usuario) : IAutorizacaoUsuarioServico
    {
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuario);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuario ?? throw new RegraNegocioException("Usuário não autenticado."));
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
