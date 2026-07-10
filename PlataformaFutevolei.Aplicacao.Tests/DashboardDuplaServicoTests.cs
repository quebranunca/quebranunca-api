using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class DashboardDuplaServicoTests
{
    [Fact]
    public async Task ObterDashboardAsync_MesmoAtleta_Bloqueia()
    {
        var atleta = CriarAtleta("Ana", "Ana");
        var cenario = new Cenario([atleta], []);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ObterDashboardAsync(atleta.Id, atleta.Id));

        Assert.Equal("Informe dois atletas diferentes para visualizar o dashboard da dupla.", excecao.Message);
    }

    [Fact]
    public async Task ObterDashboardAsync_AtletaInexistente_Bloqueia()
    {
        var atleta = CriarAtleta("Ana", "Ana");
        var cenario = new Cenario([atleta], []);

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.ObterDashboardAsync(atleta.Id, Guid.NewGuid()));

        Assert.Equal("Atleta não encontrado.", excecao.Message);
    }

    [Fact]
    public async Task ObterDashboardAsync_DuplaSemPartidas_RetornaResumoZerado()
    {
        var atleta1 = CriarAtleta("Ana Silva", "Ana");
        var atleta2 = CriarAtleta("Bruno Costa", "Bruno");
        var cenario = new Cenario([atleta1, atleta2], []);

        var resultado = await cenario.Servico.ObterDashboardAsync(atleta1.Id, atleta2.Id);

        Assert.Equal("Ana e Bruno", resultado.Dupla.Nome);
        Assert.Null(resultado.Dupla.CategoriaPrincipal);
        Assert.Equal(0, resultado.Resumo.TotalPartidas);
        Assert.Equal(0, resultado.Resumo.Aproveitamento);
        Assert.Equal(0, resultado.Resumo.SaldoPontos);
        Assert.Empty(resultado.UltimasPartidas);
        Assert.Empty(resultado.MelhoresAdversarios);
        Assert.Equal(6, resultado.Evolucao.Count);
        Assert.All(resultado.Evolucao, mes =>
        {
            Assert.False(mes.PossuiDados);
            Assert.Null(mes.AproveitamentoDados);
        });
        Assert.NotNull(resultado.EstatisticasPontos);
        Assert.False(resultado.EstatisticasPontos!.Disponivel);
        Assert.Contains(resultado.Metricas, x => x.Id == "partidas" && x.Valor == "0");
        Assert.Contains("Saldo positivo de 0 pontos.", resultado.Insights);
    }

    [Fact]
    public async Task ObterDashboardAsync_ComPartidasValidas_CalculaResumoAdversariosEInsights()
    {
        var atleta1 = CriarAtleta("Ana Silva", "Ana");
        var atleta2 = CriarAtleta("Bruno Costa", "Bruno");
        var rival1 = CriarAtleta("Carla Lima", "Carla");
        var rival2 = CriarAtleta("Daniel Rocha", "Daniel");
        var rival3 = CriarAtleta("Elisa Souza", "Elisa");
        var rival4 = CriarAtleta("Fabio Dias", "Fabio");
        var grupo = new Grupo { Nome = "Treino Praia" };
        var competicao = new Competicao { Nome = "Open Verão" };
        var categoria = new CategoriaCompeticao { Nome = "Misto", Competicao = competicao };
        var vitoriaRecente = CriarPartida(
            atleta1,
            atleta2,
            rival1,
            rival2,
            grupo,
            categoria,
            DateTime.UtcNow.AddDays(-1),
            duplaAlvoNaA: true,
            alvoVenceu: true,
            placarAlvo: 18,
            placarRival: 12);
        var derrotaAntiga = CriarPartida(
            atleta1,
            atleta2,
            rival3,
            rival4,
            grupo,
            categoria,
            DateTime.UtcNow.AddDays(-3),
            duplaAlvoNaA: false,
            alvoVenceu: false,
            placarAlvo: 14,
            placarRival: 18);
        var vitoriaSemPlacar = CriarPartida(
            atleta1,
            atleta2,
            rival1,
            rival2,
            grupo,
            categoria,
            DateTime.UtcNow.AddDays(-2),
            duplaAlvoNaA: true,
            alvoVenceu: true,
            placarAlvo: null,
            placarRival: null);

        var cenario = new Cenario(
            [atleta1, atleta2, rival1, rival2, rival3, rival4],
            [vitoriaRecente, derrotaAntiga, vitoriaSemPlacar]);

        var resultado = await cenario.Servico.ObterDashboardAsync(atleta1.Id, atleta2.Id);

        Assert.Equal("Misto", resultado.Dupla.CategoriaPrincipal);
        Assert.Equal(3, resultado.Resumo.TotalPartidas);
        Assert.Equal(2, resultado.Resumo.Vitorias);
        Assert.Equal(1, resultado.Resumo.Derrotas);
        Assert.Equal(66.7m, resultado.Resumo.Aproveitamento);
        Assert.Equal(32, resultado.Resumo.PontosPro);
        Assert.Equal(30, resultado.Resumo.PontosContra);
        Assert.Equal(2, resultado.Resumo.SaldoPontos);
        Assert.Equal(2, resultado.Resumo.MaiorSequenciaVitorias);
        Assert.Equal(2, resultado.Resumo.SequenciaAtual);
        Assert.Equal("Vitória", resultado.UltimasPartidas[0].Resultado);
        Assert.Equal(18, resultado.UltimasPartidas[0].PlacarDupla);
        Assert.Equal(12, resultado.UltimasPartidas[0].PlacarAdversarios);
        Assert.Equal("Carla", resultado.MelhoresAdversarios[0].Atletas[0].Apelido);
        Assert.Equal(2, resultado.MelhoresAdversarios[0].Partidas);
        Assert.Contains(resultado.Insights, x => x.Contains("Dupla venceu 2 dos últimos 3 jogos."));
        Assert.Contains(resultado.Insights, x => x.Contains("Maior rivalidade contra Carla e Daniel."));
        Assert.Contains(resultado.Insights, x => x.Contains("Sequência atual de 2 vitórias."));
    }

    [Fact]
    public async Task ObterDashboardAsync_OrdemInvertida_NormalizaDupla()
    {
        var atleta1 = CriarAtleta("Ana Silva", "Ana");
        var atleta2 = CriarAtleta("Bruno Costa", "Bruno");
        var rival1 = CriarAtleta("Carla Lima", "Carla");
        var rival2 = CriarAtleta("Daniel Rocha", "Daniel");
        var partida = CriarPartida(
            atleta1,
            atleta2,
            rival1,
            rival2,
            null,
            null,
            DateTime.UtcNow,
            duplaAlvoNaA: true,
            alvoVenceu: true,
            placarAlvo: 18,
            placarRival: 12);
        var cenario = new Cenario([atleta1, atleta2, rival1, rival2], [partida]);

        var resultado = await cenario.Servico.ObterDashboardAsync(atleta2.Id, atleta1.Id);

        Assert.Equal(1, resultado.Resumo.TotalPartidas);
        Assert.Equal(1, resultado.Resumo.Vitorias);
        Assert.Equal(18, resultado.EstatisticasPontos!.PontosPro);
        Assert.Equal(12, resultado.EstatisticasPontos.PontosContra);
    }

    [Fact]
    public async Task ObterDashboardAsync_IgnoraPartidasNaoEncerradasContestadasOuSemVencedor()
    {
        var atleta1 = CriarAtleta("Ana", null);
        var atleta2 = CriarAtleta("Bruno", null);
        var rival1 = CriarAtleta("Carla", null);
        var rival2 = CriarAtleta("Daniel", null);
        var valida = CriarPartida(atleta1, atleta2, rival1, rival2, null, null, DateTime.UtcNow, true, true, 18, 10);
        var agendada = CriarPartida(atleta1, atleta2, rival1, rival2, null, null, DateTime.UtcNow, true, true, 18, 8);
        agendada.Status = StatusPartida.Agendada;
        var contestada = CriarPartida(atleta1, atleta2, rival1, rival2, null, null, DateTime.UtcNow, true, true, 18, 9);
        contestada.StatusAprovacao = StatusAprovacaoPartida.Contestada;
        var semVencedora = CriarPartida(atleta1, atleta2, rival1, rival2, null, null, DateTime.UtcNow, true, true, 18, 11);
        semVencedora.DuplaVencedoraId = null;

        var cenario = new Cenario([atleta1, atleta2, rival1, rival2], [valida, agendada, contestada, semVencedora]);

        var resultado = await cenario.Servico.ObterDashboardAsync(atleta1.Id, atleta2.Id);

        Assert.Equal(1, resultado.Resumo.TotalPartidas);
        Assert.Single(resultado.UltimasPartidas);
        Assert.Equal(valida.Id, resultado.UltimasPartidas[0].Id);
    }

    [Fact]
    public async Task ObterDashboardAsync_PartidasDeOutraDupla_NaoEntramNoResumo()
    {
        var atleta1 = CriarAtleta("Ana", null);
        var atleta2 = CriarAtleta("Bruno", null);
        var outro1 = CriarAtleta("Carla", null);
        var outro2 = CriarAtleta("Daniel", null);
        var rival1 = CriarAtleta("Elisa", null);
        var rival2 = CriarAtleta("Fabio", null);
        var partidaOutraDupla = CriarPartida(outro1, outro2, rival1, rival2, null, null, DateTime.UtcNow, true, true, 18, 10);
        var cenario = new Cenario([atleta1, atleta2, outro1, outro2, rival1, rival2], [partidaOutraDupla]);

        var resultado = await cenario.Servico.ObterDashboardAsync(atleta1.Id, atleta2.Id);

        Assert.Equal(0, resultado.Resumo.TotalPartidas);
        Assert.Empty(resultado.UltimasPartidas);
        Assert.Empty(resultado.MelhoresAdversarios);
    }

    private static Atleta CriarAtleta(string nome, string? apelido)
        => new()
        {
            Nome = nome,
            Apelido = apelido
        };

    private static Partida CriarPartida(
        Atleta atleta1,
        Atleta atleta2,
        Atleta rival1,
        Atleta rival2,
        Grupo? grupo,
        CategoriaCompeticao? categoria,
        DateTime data,
        bool duplaAlvoNaA,
        bool alvoVenceu,
        int? placarAlvo,
        int? placarRival)
    {
        var duplaAlvo = new Dupla
        {
            Atleta1Id = atleta1.Id,
            Atleta1 = atleta1,
            Atleta2Id = atleta2.Id,
            Atleta2 = atleta2
        };
        var duplaRival = new Dupla
        {
            Atleta1Id = rival1.Id,
            Atleta1 = rival1,
            Atleta2Id = rival2.Id,
            Atleta2 = rival2
        };

        var duplaA = duplaAlvoNaA ? duplaAlvo : duplaRival;
        var duplaB = duplaAlvoNaA ? duplaRival : duplaAlvo;

        return new Partida
        {
            GrupoId = grupo?.Id,
            Grupo = grupo,
            CategoriaCompeticaoId = categoria?.Id,
            CategoriaCompeticao = categoria,
            Status = StatusPartida.Encerrada,
            StatusAprovacao = StatusAprovacaoPartida.Aprovada,
            DuplaAId = duplaA.Id,
            DuplaA = duplaA,
            DuplaBId = duplaB.Id,
            DuplaB = duplaB,
            DuplaVencedoraId = alvoVenceu ? duplaAlvo.Id : duplaRival.Id,
            TipoRegistroResultado = placarAlvo.HasValue && placarRival.HasValue
                ? TipoRegistroResultado.PlacarDetalhado
                : TipoRegistroResultado.ApenasResultado,
            PlacarDuplaA = duplaAlvoNaA ? placarAlvo : placarRival,
            PlacarDuplaB = duplaAlvoNaA ? placarRival : placarAlvo,
            DataPartida = data
        };
    }

    private sealed class Cenario
    {
        public Cenario(IReadOnlyList<Atleta> atletas, IReadOnlyList<Partida> partidas)
        {
            Servico = new DashboardDuplaServico(
                new AtletaRepositorioStub(atletas),
                new PartidaRepositorioStub(partidas));
        }

        public DashboardDuplaServico Servico { get; }
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
        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(partidas.Count);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(partidas);
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
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => Task.FromResult(new AtletasSugestoesPartidaDto([], []));
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(new UsuarioResumoDto("Usuario", 0, 0, 0, 0, 0, 0, 0));
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(partidas.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) { }
    }
}
